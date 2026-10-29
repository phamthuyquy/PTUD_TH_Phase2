namespace QTKD.Api.Domain;

public sealed class Contract
{
    public int Id { get; set; }

    public required string ContractNo { get; set; }

    public int CustomerId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public required string Status { get; set; }

    public Customer? Customer { get; set; }
}
