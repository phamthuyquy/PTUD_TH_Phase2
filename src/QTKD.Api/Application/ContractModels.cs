using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.Application;

public sealed record CreateContractRequest
{
    [Required, StringLength(50)]
    public required string ContractNo { get; init; }

    [Range(1, int.MaxValue)]
    public int CustomerId { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    [Range(0, 9999999999999999d)]
    public decimal? ContractValue { get; init; }

    [StringLength(1000)]
    public string? PaymentTerms { get; init; }

    [StringLength(4000)]
    public string? ServiceTerms { get; init; }
}

public sealed record UpdateContractRequest
{
    [Required, StringLength(50)]
    public required string ContractNo { get; init; }

    [Range(1, int.MaxValue)]
    public int CustomerId { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    [Range(0, 9999999999999999d)]
    public decimal? ContractValue { get; init; }

    [StringLength(1000)]
    public string? PaymentTerms { get; init; }

    [StringLength(4000)]
    public string? ServiceTerms { get; init; }

    public string? Status { get; init; }
}

public sealed record ContractSubmissionResponse(
    int ContractId,
    string Status,
    Guid ApprovalId,
    DateTimeOffset SubmittedAt,
    bool IsReplay = false);

public sealed record ContractSubmissionErrorResponse(
    string Code,
    string Message,
    IReadOnlyList<string> FailedConditions);
