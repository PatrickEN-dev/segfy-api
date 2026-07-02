using Microsoft.Extensions.Options;
using Segfy.Application.Abstractions;
using Segfy.Application.Configuration;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Application.UseCases.Policies;

public sealed class GetExpiringPoliciesUseCase(
    IPolicyRepository repo,
    IClock clock,
    IOptions<SegfyOptions> options)
{
    private readonly IPolicyRepository _repo = repo;
    private readonly IClock _clock = clock;
    private readonly IOptions<SegfyOptions> _options = options;

    public Task<IReadOnlyList<Policy>> ExecuteAsync(CancellationToken ct) =>
        _repo.ListExpiringAsync(_clock.TodayUtc, _options.Value.ExpiringWindowDays, ct);
}
