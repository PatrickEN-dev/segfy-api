namespace Segfy.Api.Contracts;

public sealed record ErrorBody(
    string Code,
    string Message,
    string RequestId,
    IReadOnlyDictionary<string, string[]>? Details);

public sealed record ErrorResponse(ErrorBody Error);
