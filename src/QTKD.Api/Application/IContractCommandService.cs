namespace QTKD.Api.Application;

public interface IContractCommandService
{
    Task<ApiOperationResult<ContractReadModel>> CreateContractAsync(
        CreateContractRequest request,
        CancellationToken cancellationToken);

    Task<ApiOperationResult<ContractReadModel>> UpdateContractAsync(
        int id,
        UpdateContractRequest request,
        CancellationToken cancellationToken);

    Task<ApiOperationResult<ContractSubmissionResponse>> SubmitContractAsync(
        int id,
        string? idempotencyKey,
        CancellationToken cancellationToken);
}
