using Microsoft.EntityFrameworkCore;
using Npgsql;
using QTKD.Api.Domain;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Application;

public sealed class CustomerService(QtkdDbContext dbContext) : ICustomerService
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

    public Task<CustomerResponse?> GetCustomerByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == id)
            .Select(customer => new CustomerResponse(
                customer.Id,
                customer.Code,
                customer.Name,
                customer.TaxCode,
                customer.Address,
                customer.RepresentativeName,
                customer.ContactInfo,
                customer.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ApiOperationResult<CustomerResponse>> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        var taxCode = request.TaxCode.Trim();
        if (await dbContext.Customers.AnyAsync(
                customer => customer.Code == code || customer.TaxCode == taxCode,
                cancellationToken))
        {
            return ApiOperationResult<CustomerResponse>.Failure(
                StatusCodes.Status409Conflict,
                "CustomerAlreadyExists",
                "Customer code or tax code is already in use.");
        }

        var customer = new Customer
        {
            Code = code,
            Name = request.Name.Trim(),
            TaxCode = taxCode,
            Address = request.Address.Trim(),
            RepresentativeName = request.RepresentativeName.Trim(),
            ContactInfo = request.ContactInfo.Trim(),
            Status = request.Status
        };

        dbContext.Customers.Add(customer);
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
            return ApiOperationResult<CustomerResponse>.Failure(
                StatusCodes.Status409Conflict,
                "CustomerAlreadyExists",
                "Customer code or tax code is already in use.");
        }

        return ApiOperationResult<CustomerResponse>.Success(ToResponse(customer));
    }

    public async Task<ApiOperationResult<CustomerResponse>> UpdateCustomerAsync(
        int id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (customer is null)
        {
            return ApiOperationResult<CustomerResponse>.Failure(
                StatusCodes.Status404NotFound,
                "CustomerNotFound",
                $"Customer {id} was not found.");
        }

        var code = request.Code.Trim();
        var taxCode = request.TaxCode.Trim();
        if (await dbContext.Customers.AnyAsync(
                item => item.Id != id
                    && (item.Code == code || item.TaxCode == taxCode),
                cancellationToken))
        {
            return ApiOperationResult<CustomerResponse>.Failure(
                StatusCodes.Status409Conflict,
                "CustomerAlreadyExists",
                "Customer code or tax code is already in use.");
        }

        customer.Code = code;
        customer.Name = request.Name.Trim();
        customer.TaxCode = taxCode;
        customer.Address = request.Address.Trim();
        customer.RepresentativeName = request.RepresentativeName.Trim();
        customer.ContactInfo = request.ContactInfo.Trim();
        customer.Status = request.Status;

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
            return ApiOperationResult<CustomerResponse>.Failure(
                StatusCodes.Status409Conflict,
                "CustomerAlreadyExists",
                "Customer code or tax code is already in use.");
        }
        return ApiOperationResult<CustomerResponse>.Success(ToResponse(customer));
    }

    private static CustomerResponse ToResponse(Customer customer) =>
        new(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.TaxCode,
            customer.Address,
            customer.RepresentativeName,
            customer.ContactInfo,
            customer.Status);
}
