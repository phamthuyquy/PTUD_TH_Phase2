namespace QTKD.Api.Domain;

public sealed class PriceItem
{
    public int PriceListId { get; set; }

    public required string ServiceCode { get; set; }

    public decimal UnitPrice { get; set; }

    public PriceList? PriceList { get; set; }
}
