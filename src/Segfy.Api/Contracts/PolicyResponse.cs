namespace Segfy.Api.Contracts;

public sealed record PolicyResponse(
    Guid Id,
    string Number,
    string Document,
    string LicensePlate,
    decimal PremiumAmount,
    DateOnly CoverageStart,
    DateOnly CoverageEnd,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
