namespace Segfy.Api.Contracts;

public sealed record CreatePolicyRequest(
    string Document,
    string LicensePlate,
    decimal PremiumAmount,
    DateOnly CoverageStart,
    DateOnly CoverageEnd);
