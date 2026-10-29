namespace QTKD.Api.Application;

public interface ICustomerReadService
{
    Task<IReadOnlyList<CustomerReadModel>> GetCustomersAsync(CancellationToken cancellationToken);
}

public interface IContractReadService
{
    Task<ContractReadModel?> GetContractByIdAsync(int id, CancellationToken cancellationToken);
}
