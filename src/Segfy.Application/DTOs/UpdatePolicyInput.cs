using Segfy.Domain.Policies;

namespace Segfy.Application.DTOs;

public sealed record UpdatePolicyInput(
    string Document,
    string LicensePlate,
    decimal PremiumAmount,
    DateOnly CoverageStart,
    DateOnly CoverageEnd,
    PolicyStatus Status,
    string? StatusReason);
