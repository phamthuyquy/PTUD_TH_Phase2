using Microsoft.EntityFrameworkCore;
using Npgsql;
using QTKD.Api.Domain;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Application;

public sealed class PricingService(QtkdDbContext dbContext) : IPricingService
{
    public async Task<IReadOnlyList<PriceListResponse>> GetPriceListsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.PriceLists
            .AsNoTracking()
            .OrderBy(priceList => priceList.Id)
            .Select(ToResponse())
            .ToListAsync(cancellationToken);
    }

    public Task<PriceListResponse?> GetPriceListByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return dbContext.PriceLists
            .AsNoTracking()
            .Where(priceList => priceList.Id == id)
            .Select(ToResponse())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ApiOperationResult<PriceListResponse>> CreatePriceListAsync(
        CreatePriceListRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Contracts.AnyAsync(
                contract => contract.Id == request.ContractId,
                cancellationToken))
        {
            return ApiOperationResult<PriceListResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "InvalidContract",
                "PriceList must reference an existing contract.");
        }

        if (request.EffectiveDate > request.EndDate)
        {
            return ApiOperationResult<PriceListResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "InvalidEffectivePeriod",
                "PriceList effective date must not be later than its end date.");
        }

        var priceList = new PriceList
        {
            Name = request.Name.Trim(),
            ContractId = request.ContractId,
            EffectiveDate = request.EffectiveDate,
            EndDate = request.EndDate
        };
        dbContext.PriceLists.Add(priceList);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiOperationResult<PriceListResponse>.Success(
            new PriceListResponse(
                priceList.Id,
                priceList.Name,
                priceList.ContractId,
                priceList.EffectiveDate,
                priceList.EndDate));
    }

    public async Task<IReadOnlyList<PriceItemResponse>?> GetPriceItemsAsync(
        int priceListId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.PriceLists.AnyAsync(
                priceList => priceList.Id == priceListId,
                cancellationToken))
        {
            return null;
        }

        return await dbContext.PriceItems
            .AsNoTracking()
            .Where(item => item.PriceListId == priceListId)
            .OrderBy(item => item.ServiceCode)
            .Select(item => new PriceItemResponse(
                item.PriceListId,
                item.ServiceCode,
                item.UnitPrice))
            .ToListAsync(cancellationToken);
    }

    public Task<PriceItemResponse?> GetPriceItemAsync(
        int priceListId,
        string serviceCode,
        CancellationToken cancellationToken)
    {
        return dbContext.PriceItems
            .AsNoTracking()
            .Where(item => item.PriceListId == priceListId
                && item.ServiceCode == serviceCode)
            .Select(item => new PriceItemResponse(
                item.PriceListId,
                item.ServiceCode,
                item.UnitPrice))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ApiOperationResult<PriceItemResponse>> CreatePriceItemAsync(
        int priceListId,
        CreatePriceItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.PriceLists.AnyAsync(
                priceList => priceList.Id == priceListId,
                cancellationToken))
        {
            return ApiOperationResult<PriceItemResponse>.Failure(
                StatusCodes.Status404NotFound,
                "PriceListNotFound",
                $"PriceList {priceListId} was not found.");
        }

        if (request.UnitPrice <= 0m)
        {
            return ApiOperationResult<PriceItemResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "InvalidUnitPrice",
                "Unit price must be greater than zero.");
        }

        var serviceCode = request.ServiceCode.Trim();
        if (await dbContext.PriceItems.AnyAsync(
                item => item.PriceListId == priceListId
                    && item.ServiceCode == serviceCode,
                cancellationToken))
        {
            return ApiOperationResult<PriceItemResponse>.Failure(
                StatusCodes.Status409Conflict,
                "PriceItemAlreadyExists",
                "Service code already exists in this price list.");
        }

        var item = new PriceItem
        {
            PriceListId = priceListId,
            ServiceCode = serviceCode,
            UnitPrice = request.UnitPrice
        };
        dbContext.PriceItems.Add(item);
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
            return ApiOperationResult<PriceItemResponse>.Failure(
                StatusCodes.Status409Conflict,
                "PriceItemAlreadyExists",
                "Service code already exists in this price list.");
        }

        return ApiOperationResult<PriceItemResponse>.Success(
            new PriceItemResponse(item.PriceListId, item.ServiceCode, item.UnitPrice));
    }

    private static System.Linq.Expressions.Expression<Func<PriceList, PriceListResponse>>
        ToResponse() =>
        priceList => new PriceListResponse(
            priceList.Id,
            priceList.Name,
            priceList.ContractId,
            priceList.EffectiveDate,
            priceList.EndDate);
}
