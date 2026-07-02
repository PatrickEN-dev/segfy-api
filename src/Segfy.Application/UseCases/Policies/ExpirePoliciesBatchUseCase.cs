using Segfy.Application.Abstractions;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Application.UseCases.Policies;

public sealed class ExpirePoliciesBatchUseCase(IPolicyRepository repo, IClock clock)
{
    private readonly IPolicyRepository _repo = repo;
    private readonly IClock _clock = clock;

    public const string AutomatedReason = "Auto-expired: coverage period ended.";

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var today = _clock.TodayUtc;
        var policies = await _repo.ListActiveExpiredAsync(today, ct);
        if (policies.Count == 0) return 0;

        var now = _clock.UtcNow;
        foreach (var policy in policies)
            policy.ChangeStatus(PolicyStatus.Expirada, now, AutomatedReason);

        await _repo.SaveExpirationBatchAsync(policies, ct);
        return policies.Count;
    }
}
