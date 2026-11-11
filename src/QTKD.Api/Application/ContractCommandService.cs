using Microsoft.EntityFrameworkCore;
using Npgsql;
using QTKD.Api.Domain;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Application;

public sealed class ContractCommandService(
    QtkdDbContext dbContext,
    TimeProvider timeProvider) : IContractCommandService
{
    private sealed record ContractSubmissionCandidate(
        int CustomerId,
        DateOnly StartDate,
        DateOnly EndDate,
        string? ServiceTerms);

    public async Task<ApiOperationResult<ContractReadModel>> CreateContractAsync(
        CreateContractRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsActiveCustomerAsync(request.CustomerId, cancellationToken))
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status400BadRequest,
                "InvalidCustomer",
                "Contract must reference an active customer.");
        }

        var contractNo = request.ContractNo.Trim();
        if (await dbContext.Contracts.AnyAsync(
                contract => contract.ContractNo == contractNo,
                cancellationToken))
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status409Conflict,
                "ContractAlreadyExists",
                "Contract number is already in use.");
        }

        var contract = new Contract
        {
            ContractNo = contractNo,
            CustomerId = request.CustomerId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ContractValue = request.ContractValue,
            PaymentTerms = request.PaymentTerms?.Trim(),
            ServiceTerms = request.ServiceTerms?.Trim(),
            Status = ContractStatuses.Draft
        };

        dbContext.Contracts.Add(contract);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status409Conflict,
                "ContractAlreadyExists",
                "Contract number is already in use.");
        }

        return ApiOperationResult<ContractReadModel>.Success(
            await GetContractReadModelAsync(contract.Id, cancellationToken));
    }

    public async Task<ApiOperationResult<ContractReadModel>> UpdateContractAsync(
        int id,
        UpdateContractRequest request,
        CancellationToken cancellationToken)
    {
        if (!ContractBusinessRules.CanSetStatusDirectly(request.Status))
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status400BadRequest,
                "ContractStatusManagedByWorkflow",
                "Contract status is managed by the contract workflow and cannot be changed directly.");
        }

        var existingContract = await dbContext.Contracts
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (existingContract is null)
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status404NotFound,
                "ContractNotFound",
                $"Contract {id} was not found.");
        }

        if (!ContractBusinessRules.CanEdit(existingContract.Status))
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status409Conflict,
                "ContractNotEditable",
                "Only Draft or Revision Requested contracts can be edited.");
        }

        if (!await IsActiveCustomerAsync(request.CustomerId, cancellationToken))
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status400BadRequest,
                "InvalidCustomer",
                "Contract must reference an active customer.");
        }

        var contractNo = request.ContractNo.Trim();
        if (await dbContext.Contracts.AnyAsync(
                item => item.Id != id && item.ContractNo == contractNo,
                cancellationToken))
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status409Conflict,
                "ContractAlreadyExists",
                "Contract number is already in use.");
        }

        int updatedCount;
        try
        {
            updatedCount = await dbContext.Contracts
                .Where(item => item.Id == id
                    && (item.Status == ContractStatuses.Draft
                        || item.Status == ContractStatuses.RevisionRequested))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(item => item.ContractNo, contractNo)
                        .SetProperty(item => item.CustomerId, request.CustomerId)
                        .SetProperty(item => item.StartDate, request.StartDate)
                        .SetProperty(item => item.EndDate, request.EndDate)
                        .SetProperty(item => item.ContractValue, request.ContractValue)
                        .SetProperty(item => item.PaymentTerms, request.PaymentTerms == null
                            ? null
                            : request.PaymentTerms.Trim())
                        .SetProperty(item => item.ServiceTerms, request.ServiceTerms == null
                            ? null
                            : request.ServiceTerms.Trim()),
                    cancellationToken);
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status409Conflict,
                "ContractAlreadyExists",
                "Contract number is already in use.");
        }

        if (updatedCount == 0)
        {
            return ApiOperationResult<ContractReadModel>.Failure(
                StatusCodes.Status409Conflict,
                "ContractNotEditable",
                "Contract state changed and it can no longer be edited.");
        }

        return ApiOperationResult<ContractReadModel>.Success(
            await GetContractReadModelAsync(id, cancellationToken));
    }

    public async Task<ApiOperationResult<ContractSubmissionResponse>> SubmitContractAsync(
        int id,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var normalizedKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim();

        if (normalizedKey is { Length: > 100 })
        {
            return ApiOperationResult<ContractSubmissionResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "InvalidIdempotencyKey",
                "Idempotency-Key must not exceed 100 characters.");
        }

        if (normalizedKey is not null)
        {
            var replay = await dbContext.ContractSubmissions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    submission => submission.IdempotencyKey == normalizedKey,
                    cancellationToken);
            if (replay is not null)
            {
                if (replay.ContractId != id)
                {
                    return ApiOperationResult<ContractSubmissionResponse>.Failure(
                        StatusCodes.Status409Conflict,
                        "IdempotencyKeyConflict",
                        "Idempotency-Key has already been used for another contract.");
                }

                return ApiOperationResult<ContractSubmissionResponse>.Success(
                    ToSubmissionResponse(replay, isReplay: true));
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        var contract = await dbContext.Contracts
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.CustomerId,
                item.StartDate,
                item.EndDate,
                item.ServiceTerms,
                item.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (contract is null)
        {
            return ApiOperationResult<ContractSubmissionResponse>.Failure(
                StatusCodes.Status404NotFound,
                "ContractNotFound",
                $"Contract {id} was not found.");
        }

        if (!ContractBusinessRules.CanSubmit(contract.Status))
        {
            return ApiOperationResult<ContractSubmissionResponse>.Failure(
                StatusCodes.Status409Conflict,
                "ContractNotSubmittable",
                "Only Draft contracts can be submitted.");
        }

        var failedConditions = await GetFailedConditionsAsync(
            new ContractSubmissionCandidate(
                contract.CustomerId,
                contract.StartDate,
                contract.EndDate,
                contract.ServiceTerms),
            cancellationToken);
        if (failedConditions.Count > 0)
        {
            return new ApiOperationResult<ContractSubmissionResponse>(
                null,
                StatusCodes.Status400BadRequest,
                "ContractDoesNotMeetCtr02",
                "Contract does not meet the submission requirements.",
                failedConditions);
        }

        var updatedCount = await dbContext.Contracts
            .Where(item => item.Id == id && item.Status == ContractStatuses.Draft)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.Status,
                        ContractBusinessRules.GetStatusAfterSubmission()),
                cancellationToken);

        if (updatedCount == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ApiOperationResult<ContractSubmissionResponse>.Failure(
                StatusCodes.Status409Conflict,
                "ContractNotSubmittable",
                "Contract was submitted by another request or is no longer in Draft.");
        }

        var submission = new ContractSubmission
        {
            ApprovalId = Guid.NewGuid(),
            ContractId = id,
            IdempotencyKey = normalizedKey,
            SubmittedAt = timeProvider.GetUtcNow()
        };
        dbContext.ContractSubmissions.Add(submission);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            await transaction.RollbackAsync(cancellationToken);
            return ApiOperationResult<ContractSubmissionResponse>.Failure(
                StatusCodes.Status409Conflict,
                "DuplicateContractSubmission",
                "A submission already exists for this contract or idempotency key.");
        }

        return ApiOperationResult<ContractSubmissionResponse>.Success(
            ToSubmissionResponse(submission, isReplay: false));
    }

    private async Task<IReadOnlyList<string>> GetFailedConditionsAsync(
        ContractSubmissionCandidate contract,
        CancellationToken cancellationToken)
    {
        var customerIsValid = await dbContext.Customers.AnyAsync(
            customer => customer.Id == contract.CustomerId
                && customer.Status == "Active",
            cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return ContractBusinessRules.GetFailedSubmissionConditions(
            customerIsValid,
            contract.StartDate,
            contract.EndDate,
            today,
            contract.ServiceTerms);
    }

    private Task<bool> IsActiveCustomerAsync(int customerId, CancellationToken cancellationToken) =>
        dbContext.Customers.AnyAsync(
            customer => customer.Id == customerId && customer.Status == "Active",
            cancellationToken);

    private Task<ContractReadModel> GetContractReadModelAsync(
        int id,
        CancellationToken cancellationToken) =>
        dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.Id == id)
            .Select(contract => new ContractReadModel(
                contract.Id,
                contract.ContractNo,
                contract.CustomerId,
                contract.Customer!.Name,
                contract.StartDate,
                contract.EndDate,
                contract.ContractValue,
                contract.PaymentTerms,
                contract.ServiceTerms,
                contract.Status))
            .SingleAsync(cancellationToken);

    private static ContractSubmissionResponse ToSubmissionResponse(
        ContractSubmission submission,
        bool isReplay) =>
        new(
            submission.ContractId,
            ContractStatuses.PendingApproval,
            submission.ApprovalId,
            submission.SubmittedAt,
            isReplay);
}
