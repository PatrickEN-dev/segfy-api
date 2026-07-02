namespace Segfy.Application.DTOs;

public sealed record CreatePolicyInput(
    string Document,
    string LicensePlate,
    decimal PremiumAmount,
    DateOnly CoverageStart,
    DateOnly CoverageEnd);
