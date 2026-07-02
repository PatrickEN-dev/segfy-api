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
        var expired = 0;
        foreach (var policy in policies)
        {
            policy.ChangeStatus(PolicyStatus.Expirada, _clock.UtcNow, AutomatedReason);
            await _repo.UpdateAsync(policy, ct);
            expired++;
        }
        return expired;
    }
}
