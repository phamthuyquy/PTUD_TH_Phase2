namespace QTKD.Api.Application;

public interface ICustomerService : ICustomerReadService
{
    Task<CustomerResponse?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken);

    Task<ApiOperationResult<CustomerResponse>> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken);

    Task<ApiOperationResult<CustomerResponse>> UpdateCustomerAsync(
        int id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken);
}
