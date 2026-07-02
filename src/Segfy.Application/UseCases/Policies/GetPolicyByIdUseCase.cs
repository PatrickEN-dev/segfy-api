using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Application.UseCases.Policies;

public sealed class GetPolicyByIdUseCase(IPolicyRepository repo)
{
    private readonly IPolicyRepository _repo = repo;

    public async Task<Policy> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var policy = await _repo.FindByIdAsync(id, ct)
            ?? throw new DomainNotFoundException($"Policy {id} not found.");
        return policy;
    }
}
