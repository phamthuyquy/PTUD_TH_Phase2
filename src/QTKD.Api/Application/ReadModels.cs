namespace QTKD.Api.Application;

public sealed record CustomerReadModel(int Id, string Name, string Status);

public sealed record ContractReadModel(
    int Id,
    string ContractNo,
    int CustomerId,
    string CustomerName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status);
