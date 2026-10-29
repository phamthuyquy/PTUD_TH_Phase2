namespace QTKD.Api.Domain;

public sealed class Customer
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Status { get; set; }
}
