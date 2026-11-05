using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.Application;

public sealed record CustomerResponse(
    int Id,
    string Code,
    string Name,
    string TaxCode,
    string Address,
    string RepresentativeName,
    string ContactInfo,
    string Status);

public sealed record CreateCustomerRequest
{
    [Required, StringLength(30)]
    public required string Code { get; init; }

    [Required, StringLength(200)]
    public required string Name { get; init; }

    [Required, StringLength(30)]
    public required string TaxCode { get; init; }

    [Required, StringLength(300)]
    public required string Address { get; init; }

    [Required, StringLength(150)]
    public required string RepresentativeName { get; init; }

    [Required, StringLength(200)]
    public required string ContactInfo { get; init; }

    [Required, RegularExpression("^(Active|Suspended)$")]
    public required string Status { get; init; }
}

public sealed record UpdateCustomerRequest
{
    [Required, StringLength(30)]
    public required string Code { get; init; }

    [Required, StringLength(200)]
    public required string Name { get; init; }

    [Required, StringLength(30)]
    public required string TaxCode { get; init; }

    [Required, StringLength(300)]
    public required string Address { get; init; }

    [Required, StringLength(150)]
    public required string RepresentativeName { get; init; }

    [Required, StringLength(200)]
    public required string ContactInfo { get; init; }

    [Required, RegularExpression("^(Active|Suspended)$")]
    public required string Status { get; init; }
}
