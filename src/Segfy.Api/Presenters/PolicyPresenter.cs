using Segfy.Api.Contracts;
using Segfy.Domain.Policies;

namespace Segfy.Api.Presenters;

public static class PolicyPresenter
{
    public static PolicyResponse ToResponse(Policy p) => new(
        p.Id,
        p.Number.Value,
        p.Document.Digits,
        p.LicensePlate.Value,
        p.Premium.Amount,
        p.Coverage.Start,
        p.Coverage.End,
        p.Status.ToString(),
        p.CreatedAt,
        p.UpdatedAt);

    public static IReadOnlyList<PolicyResponse> ToResponseList(IEnumerable<Policy> policies) =>
        policies.Select(ToResponse).ToList();

    public static StatusHistoryEntryResponse ToHistoryEntry(PolicyStatusHistory h) => new(
        h.Id,
        h.FromStatus.ToString(),
        h.ToStatus.ToString(),
        h.Reason,
        h.ChangedAt);
}
