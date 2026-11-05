using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.Application;

public sealed record CreatePriceListRequest
{
    [Required, StringLength(200)]
    public required string Name { get; init; }

    [Range(1, int.MaxValue)]
    public int ContractId { get; init; }

    public DateOnly EffectiveDate { get; init; }

    public DateOnly EndDate { get; init; }
}

public sealed record PriceListResponse(
    int Id,
    string Name,
    int ContractId,
    DateOnly EffectiveDate,
    DateOnly EndDate);

public sealed record CreatePriceItemRequest
{
    [Required, StringLength(50)]
    public required string ServiceCode { get; init; }

    [Range(0.01, 9999999999999999d)]
    public decimal UnitPrice { get; init; }
}

public sealed record PriceItemResponse(
    int PriceListId,
    string ServiceCode,
    decimal UnitPrice);
