using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Application.Tests.Fakes;

public sealed class InMemoryPolicyRepository : IPolicyRepository
{
    private readonly List<Policy> _policies = new();

    public IReadOnlyList<Policy> Snapshot => _policies;

    public Task AddAsync(Policy policy, CancellationToken ct)
    {
        _policies.Add(policy);
        return Task.CompletedTask;
    }

    public Task<Policy?> FindByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult<Policy?>(_policies.FirstOrDefault(p => p.Id == id));

    public Task<(IReadOnlyList<Policy> Items, int Total)> ListAsync(PolicyListQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<Policy> q = _policies;

        if (query.Status is { } status)
            q = q.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(query.DocumentContains))
            q = q.Where(p => p.Document.Digits.Contains(query.DocumentContains.Trim(), StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(query.LicensePlateContains))
            q = q.Where(p => p.LicensePlate.Value.Contains(
                query.LicensePlateContains.Trim().ToUpperInvariant(), StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(query.NumberContains))
            q = q.Where(p => p.Number.Value.Contains(
                query.NumberContains.Trim().ToUpperInvariant(), StringComparison.Ordinal));

        var filtered = q.ToList();
        var total = filtered.Count;

        IOrderedEnumerable<Policy> ordered = (query.SortBy, query.SortDir) switch
        {
            (PolicySortField.CoverageEnd, SortDirection.Asc) => filtered.OrderBy(p => p.CoverageEnd),
            (PolicySortField.CoverageEnd, SortDirection.Desc) => filtered.OrderByDescending(p => p.CoverageEnd),
            (PolicySortField.Premium, SortDirection.Asc) => filtered.OrderBy(p => p.Premium.Amount),
            (PolicySortField.Premium, SortDirection.Desc) => filtered.OrderByDescending(p => p.Premium.Amount),
            (PolicySortField.CreatedAt, SortDirection.Asc) => filtered.OrderBy(p => p.CreatedAt),
            _ => filtered.OrderByDescending(p => p.CreatedAt),
        };

        IReadOnlyList<Policy> items = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult((items, total));
    }

    public Task UpdateAsync(Policy policy, CancellationToken ct) => Task.CompletedTask;

    public Task RemoveAsync(Policy policy, CancellationToken ct)
    {
        _policies.Remove(policy);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Policy>> ListExpiringAsync(DateOnly today, int daysWindow, CancellationToken ct)
    {
        var cutoff = today.AddDays(daysWindow);
        IReadOnlyList<Policy> result = _policies
            .Where(p => p.Status == PolicyStatus.Ativa
                        && p.CoverageEnd >= today
                        && p.CoverageEnd <= cutoff)
            .OrderBy(p => p.CoverageEnd)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsActiveByPlateAsync(string plateValue, Guid? excludePolicyId, CancellationToken ct) =>
        Task.FromResult(_policies.Any(p =>
            p.Status == PolicyStatus.Ativa
            && string.Equals(p.LicensePlate.Value, plateValue, StringComparison.Ordinal)
            && (excludePolicyId is null || p.Id != excludePolicyId)));

    public Task<IReadOnlyList<Policy>> ListActiveExpiredAsync(DateOnly today, CancellationToken ct)
    {
        IReadOnlyList<Policy> result = _policies
            .Where(p => p.Status == PolicyStatus.Ativa && p.CoverageEnd < today)
            .OrderBy(p => p.CoverageEnd)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<PolicyStatusHistory>> ListStatusHistoryAsync(Guid policyId, CancellationToken ct)
    {
        var policy = _policies.FirstOrDefault(p => p.Id == policyId);
        IReadOnlyList<PolicyStatusHistory> result = policy is null
            ? Array.Empty<PolicyStatusHistory>()
            : policy.StatusHistory.OrderBy(h => h.ChangedAt).ToList();
        return Task.FromResult(result);
    }
}
