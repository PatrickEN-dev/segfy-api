namespace Segfy.Domain.Policies.Abstractions;

public interface IPolicyRepository
{
    Task AddAsync(Policy policy, CancellationToken ct);
    Task<Policy?> FindByIdAsync(Guid id, CancellationToken ct);
    Task<(IReadOnlyList<Policy> Items, int Total)> ListAsync(PolicyListQuery query, CancellationToken ct);
    Task UpdateAsync(Policy policy, CancellationToken ct);
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Policy>> ListExpiringAsync(DateOnly today, int daysWindow, CancellationToken ct);
    Task<bool> ExistsActiveByPlateAsync(string plateValue, Guid? excludePolicyId, CancellationToken ct);
    Task<IReadOnlyList<Policy>> ListActiveExpiredAsync(DateOnly today, CancellationToken ct);
    Task<IReadOnlyList<PolicyStatusHistory>> ListStatusHistoryAsync(Guid policyId, CancellationToken ct);
    Task SaveExpirationBatchAsync(IReadOnlyCollection<Policy> policies, CancellationToken ct);
}
