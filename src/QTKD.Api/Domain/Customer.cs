namespace QTKD.Api.Domain;

public sealed class Customer
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string TaxCode { get; set; }

    public required string Address { get; set; }

    public required string RepresentativeName { get; set; }

    public required string ContactInfo { get; set; }

    public required string Status { get; set; }
}
