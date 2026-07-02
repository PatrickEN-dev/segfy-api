namespace Segfy.Api.Contracts;

public sealed record UpdatePolicyRequest(
    string Document,
    string LicensePlate,
    decimal PremiumAmount,
    DateOnly CoverageStart,
    DateOnly CoverageEnd,
    string Status,
    string? StatusReason) : IPolicyPayload;
