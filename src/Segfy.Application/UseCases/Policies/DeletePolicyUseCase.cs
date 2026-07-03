using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Application.UseCases.Policies;

public sealed class DeletePolicyUseCase(IPolicyRepository repo)
{
    private readonly IPolicyRepository _repo = repo;

    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var deleted = await _repo.DeleteByIdAsync(id, ct);
        if (!deleted)
            throw new DomainNotFoundException($"Policy {id} not found.");
    }
}
