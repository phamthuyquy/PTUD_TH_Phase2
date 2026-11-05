namespace QTKD.Api.Application;

public sealed record ApiOperationResult<T>(
    T? Value,
    int StatusCode,
    string? Code = null,
    string? Message = null,
    IReadOnlyList<string>? Errors = null)
{
    public bool Succeeded => StatusCode < 400;

    public static ApiOperationResult<T> Success(T value) =>
        new(value, StatusCodes.Status200OK);

    public static ApiOperationResult<T> Failure(
        int statusCode,
        string code,
        string message,
        IReadOnlyList<string>? errors = null) =>
        new(default, statusCode, code, message, errors);
}
