using Microsoft.EntityFrameworkCore;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Application;

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
                contract.ContractValue,
                contract.PaymentTerms,
                contract.ServiceTerms,
                contract.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
