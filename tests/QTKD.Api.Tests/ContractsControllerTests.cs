using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Application;
using QTKD.Api.Controllers;

namespace QTKD.Api.Tests;

public sealed class ContractsControllerTests
{
    [Fact]
    public async Task GetContract_WhenContractExists_ReturnsContract()
    {
        var contract = new ContractReadModel(
            1,
            "HD-2026-018",
            1,
            "Công ty Minh Phát",
            new DateOnly(2026, 10, 1),
            new DateOnly(2027, 9, 30),
            10000000m,
            "Thanh toán theo tháng",
            "Dịch vụ khai thác cảng theo thỏa thuận.",
            "Draft");
        var controller = new ContractsController(
            new StubContractReadService(contract),
            new StubContractCommandService());

        var actionResult = await controller.GetContract(1, CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ContractReadModel>(result.Value);
        Assert.Equal(contract, response);
    }

    [Fact]
    public async Task GetContract_WhenContractDoesNotExist_ReturnsNotFound()
    {
        var controller = new ContractsController(
            new StubContractReadService(null),
            new StubContractCommandService());

        var actionResult = await controller.GetContract(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task SubmitContract_WhenCtr02Fails_ReturnsBadRequestWithFailedConditions()
    {
        var failure = new ApiOperationResult<ContractSubmissionResponse>(
            null,
            StatusCodes.Status400BadRequest,
            "ContractDoesNotMeetCtr02",
            "Contract does not meet the submission requirements.",
            ["ServiceTerms is required."]);
        var controller = new ContractsController(
            new StubContractReadService(null),
            new StubContractCommandService(failure));

        var actionResult = await controller.SubmitContract(
            1,
            "demo-key",
            CancellationToken.None);

        var result = Assert.IsType<BadRequestObjectResult>(actionResult);
        var response = Assert.IsType<ContractSubmissionErrorResponse>(result.Value);
        Assert.Equal("ContractDoesNotMeetCtr02", response.Code);
        Assert.Equal(new[] { "ServiceTerms is required." }, response.FailedConditions);
    }

    private sealed class StubContractReadService(
        ContractReadModel? contract) : IContractReadService
    {
        public Task<ContractReadModel?> GetContractByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(contract);
        }
    }

    private sealed class StubContractCommandService(
        ApiOperationResult<ContractSubmissionResponse>? submissionResult = null)
        : IContractCommandService
    {
        public Task<ApiOperationResult<ContractReadModel>> CreateContractAsync(
            CreateContractRequest request,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApiOperationResult<ContractReadModel>> UpdateContractAsync(
            int id,
            UpdateContractRequest request,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApiOperationResult<ContractSubmissionResponse>> SubmitContractAsync(
            int id,
            string? idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(submissionResult
                ?? throw new InvalidOperationException("No submission result was configured."));
    }
}
