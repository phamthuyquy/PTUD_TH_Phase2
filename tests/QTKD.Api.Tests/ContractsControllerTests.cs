using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QTKD.Api.Application;
using QTKD.Api.Domain;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ContractsControllerTests : IDisposable
{
    private readonly IntegrationApiFactory _factory;
    private readonly HttpClient _client;

    public ContractsControllerTests()
    {
        _factory = new IntegrationApiFactory();
        _client = _factory.CreateClient();
        _factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task ContractWorkflow_CreatesSubmitsReadsStateAndPreventsDuplicates_TC03_TC04_TC05_TC06_TC07_TC08()
    {
        var customerRequest = new CreateCustomerRequest
        {
            Code = $"KH-{Guid.NewGuid():N}"[..23],
            Name = "Công ty kiểm thử",
            TaxCode = Guid.NewGuid().ToString("N")[..30],
            Address = "TP. Hồ Chí Minh",
            RepresentativeName = "Nguyễn An",
            ContactInfo = "0909000000",
            Status = "Active"
        };
        var customerResponse = await _client.PostAsJsonAsync("/api/customers", customerRequest);

        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var contractRequest = new CreateContractRequest
        {
            ContractNo = $"HD-TEST-{Guid.NewGuid():N}",
            CustomerId = customer.Id,
            StartDate = today,
            EndDate = today.AddYears(1),
            ServiceTerms = ""
        };
        var createResponse = await _client.PostAsJsonAsync("/api/contracts", contractRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdContract = await createResponse.Content.ReadFromJsonAsync<ContractReadModel>();
        Assert.NotNull(createdContract);
        Assert.Equal(ContractStatuses.Draft, createdContract.Status);
        Assert.Equal(customer.Id, createdContract.CustomerId);
        Assert.Equal(customer.Name, createdContract.CustomerName);

        var idempotencyKey = $"week07-{Guid.NewGuid():N}";
        using var submitRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/contracts/{createdContract.Id}/submit");
        submitRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var failedSubmitResponse = await _client.SendAsync(submitRequest);

        Assert.Equal(HttpStatusCode.BadRequest, failedSubmitResponse.StatusCode);
        var submissionError =
            await failedSubmitResponse.Content.ReadFromJsonAsync<ContractSubmissionErrorResponse>();
        Assert.NotNull(submissionError);
        Assert.Equal("ContractDoesNotMeetCtr02", submissionError.Code);
        Assert.Contains(submissionError.FailedConditions, condition =>
            condition.Contains("ServiceTerms", StringComparison.Ordinal));
        using (var failedSubmitScope = _factory.Services.CreateScope())
        {
            var failedSubmitDb = failedSubmitScope.ServiceProvider
                .GetRequiredService<QtkdDbContext>();
            Assert.Equal(
                0,
                await failedSubmitDb.ContractSubmissions.CountAsync(item =>
                    item.ContractId == createdContract.Id));
        }

        var contractUrl = $"/api/contracts/{createdContract.Id}";
        var afterFailedSubmit = await _client.GetFromJsonAsync<ContractReadModel>(contractUrl);
        Assert.NotNull(afterFailedSubmit);
        Assert.Equal(ContractStatuses.Draft, afterFailedSubmit.Status);

        var attemptedApproval = new
        {
            contractRequest.ContractNo,
            contractRequest.CustomerId,
            contractRequest.StartDate,
            contractRequest.EndDate,
            contractRequest.ContractValue,
            contractRequest.PaymentTerms,
            contractRequest.ServiceTerms,
            Status = ContractStatuses.Approved
        };
        var directApprovalResponse = await _client.PutAsJsonAsync(contractUrl, attemptedApproval);

        Assert.Equal(HttpStatusCode.BadRequest, directApprovalResponse.StatusCode);
        var directApprovalProblem =
            await directApprovalResponse.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(directApprovalProblem);
        Assert.Equal("ContractStatusManagedByWorkflow", directApprovalProblem.Title);
        var afterRejectedApproval = await _client.GetFromJsonAsync<ContractReadModel>(contractUrl);
        Assert.NotNull(afterRejectedApproval);
        Assert.Equal(ContractStatuses.Draft, afterRejectedApproval.Status);

        var completedRequest = new UpdateContractRequest
        {
            ContractNo = contractRequest.ContractNo,
            CustomerId = customer.Id,
            StartDate = today,
            EndDate = today.AddYears(1),
            ContractValue = 10000000m,
            PaymentTerms = "Thanh toán theo tháng",
            ServiceTerms = "Dịch vụ khai thác cảng theo thỏa thuận."
        };
        var updateResponse = await _client.PutAsJsonAsync(contractUrl, completedRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedContract = await updateResponse.Content.ReadFromJsonAsync<ContractReadModel>();
        Assert.NotNull(updatedContract);
        Assert.Equal(ContractStatuses.Draft, updatedContract.Status);
        Assert.Equal(completedRequest.ServiceTerms, updatedContract.ServiceTerms);

        using var validSubmitRequest = new HttpRequestMessage(HttpMethod.Post, $"{contractUrl}/submit");
        validSubmitRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var submitResponse = await _client.SendAsync(validSubmitRequest);

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var submission = await submitResponse.Content.ReadFromJsonAsync<ContractSubmissionResponse>();
        Assert.NotNull(submission);
        Assert.Equal(createdContract.Id, submission.ContractId);
        Assert.Equal(ContractStatuses.PendingApproval, submission.Status);
        Assert.NotEqual(Guid.Empty, submission.ApprovalId);
        Assert.False(submission.IsReplay);

        var afterSuccessfulSubmit = await _client.GetFromJsonAsync<ContractReadModel>(contractUrl);
        Assert.NotNull(afterSuccessfulSubmit);
        Assert.Equal(ContractStatuses.PendingApproval, afterSuccessfulSubmit.Status);

        var attemptedPendingEdit = new UpdateContractRequest
        {
            ContractNo = $"{contractRequest.ContractNo}-EDIT",
            CustomerId = customer.Id,
            StartDate = today,
            EndDate = today.AddYears(1),
            ServiceTerms = "Không được sửa sau submit."
        };
        var pendingEditResponse = await _client.PutAsJsonAsync(contractUrl, attemptedPendingEdit);

        Assert.Equal(HttpStatusCode.Conflict, pendingEditResponse.StatusCode);
        var afterRejectedEdit = await _client.GetFromJsonAsync<ContractReadModel>(contractUrl);
        Assert.NotNull(afterRejectedEdit);
        Assert.Equal(contractRequest.ContractNo, afterRejectedEdit.ContractNo);
        Assert.Equal(ContractStatuses.PendingApproval, afterRejectedEdit.Status);

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, $"{contractUrl}/submit");
        replayRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var replayResponse = await _client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replay = await replayResponse.Content.ReadFromJsonAsync<ContractSubmissionResponse>();
        Assert.NotNull(replay);
        Assert.True(replay.IsReplay);
        Assert.Equal(submission.ApprovalId, replay.ApprovalId);

        var duplicateResponse = await _client.PostAsync($"{contractUrl}/submit", null);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        Assert.Equal(
            1,
            await dbContext.ContractSubmissions.CountAsync(item =>
                item.ContractId == createdContract.Id));
    }

    [Fact]
    public async Task SubmitContract_InvalidId_ReturnsNotFoundProblem()
    {
        var response = await _client.PostAsync("/api/contracts/99999/submit", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("ContractNotFound", problem.Title);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
