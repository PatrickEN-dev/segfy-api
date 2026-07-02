using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Application.UseCases.Policies;

public sealed class GetPolicyStatusHistoryUseCase(IPolicyRepository repo)
{
    private readonly IPolicyRepository _repo = repo;

    public async Task<IReadOnlyList<PolicyStatusHistory>> ExecuteAsync(Guid policyId, CancellationToken ct)
    {
        var policy = await _repo.FindByIdAsync(policyId, ct)
            ?? throw new DomainNotFoundException($"Policy {policyId} not found.");
        return await _repo.ListStatusHistoryAsync(policy.Id, ct);
    }
}
