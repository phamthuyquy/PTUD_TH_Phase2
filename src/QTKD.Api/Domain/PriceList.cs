namespace QTKD.Api.Domain;

public sealed class PriceList
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int ContractId { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public DateOnly EndDate { get; set; }

    public ICollection<PriceItem> Items { get; set; } = new List<PriceItem>();
}
