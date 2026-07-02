using Segfy.Application.Common;
using Segfy.Application.DTOs;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Application.UseCases.Policies;

public sealed class ListPoliciesUseCase(IPolicyRepository repo)
{
    private readonly IPolicyRepository _repo = repo;

    public async Task<PaginatedResult<Policy>> ExecuteAsync(ListPoliciesInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);

        var page = Math.Max(1, input.Page);
        var pageSize = Math.Clamp(input.PageSize, 1, 100);

        var query = new PolicyListQuery(
            page,
            pageSize,
            input.Status,
            input.Document,
            input.LicensePlate,
            input.Number,
            input.SortBy,
            input.SortDir);

        var (items, total) = await _repo.ListAsync(query, ct);
        return new PaginatedResult<Policy>(items, page, pageSize, total);
    }
}
