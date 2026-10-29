using Microsoft.EntityFrameworkCore;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Application;

public sealed class CustomerReadService(QtkdDbContext dbContext) : ICustomerReadService
{
    public async Task<IReadOnlyList<CustomerReadModel>> GetCustomersAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Id)
            .Select(customer => new CustomerReadModel(
                customer.Id,
                customer.Name,
                customer.Status))
            .ToListAsync(cancellationToken);
    }
}

public sealed class ContractReadService(QtkdDbContext dbContext) : IContractReadService
{
    public Task<ContractReadModel?> GetContractByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.Id == id)
            .Select(contract => new ContractReadModel(
                contract.Id,
                contract.ContractNo,
                contract.CustomerId,
                contract.Customer!.Name,
                contract.StartDate,
                contract.EndDate,
                contract.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
