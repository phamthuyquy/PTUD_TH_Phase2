namespace QTKD.Api.Application;

public sealed record CustomerReadModel(int Id, string Name, string Status);

public sealed record ContractReadModel(
    int Id,
    string ContractNo,
    int CustomerId,
    string CustomerName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? ContractValue,
    string? PaymentTerms,
    string? ServiceTerms,
    string Status);
