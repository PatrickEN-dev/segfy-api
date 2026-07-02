using Segfy.Application.Common;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Application.UseCases.Policies;

public sealed class ListPoliciesUseCase(IPolicyRepository repo)
{
    private readonly IPolicyRepository _repo = repo;

    public async Task<PaginatedResult<Policy>> ExecuteAsync(PolicyListQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 100),
        };

        var (items, total) = await _repo.ListAsync(normalized, ct);
        return new PaginatedResult<Policy>(items, normalized.Page, normalized.PageSize, total);
    }
}
