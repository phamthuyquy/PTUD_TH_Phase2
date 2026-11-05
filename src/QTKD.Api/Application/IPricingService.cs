namespace QTKD.Api.Application;

public interface IPricingService
{
    Task<IReadOnlyList<PriceListResponse>> GetPriceListsAsync(CancellationToken cancellationToken);

    Task<PriceListResponse?> GetPriceListByIdAsync(int id, CancellationToken cancellationToken);

    Task<ApiOperationResult<PriceListResponse>> CreatePriceListAsync(
        CreatePriceListRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PriceItemResponse>?> GetPriceItemsAsync(
        int priceListId,
        CancellationToken cancellationToken);

    Task<PriceItemResponse?> GetPriceItemAsync(
        int priceListId,
        string serviceCode,
        CancellationToken cancellationToken);

    Task<ApiOperationResult<PriceItemResponse>> CreatePriceItemAsync(
        int priceListId,
        CreatePriceItemRequest request,
        CancellationToken cancellationToken);
}
