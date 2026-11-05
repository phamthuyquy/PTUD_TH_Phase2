namespace QTKD.Api.Domain;

public sealed class ContractSubmission
{
    public int Id { get; set; }

    public Guid ApprovalId { get; set; }

    public int ContractId { get; set; }

    public string? IdempotencyKey { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
}
