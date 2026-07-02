namespace Segfy.Api.Contracts;

public sealed record PageMeta(int Page, int PageSize, int Total, int TotalPages);

public sealed record PaginatedPoliciesResponse(IReadOnlyList<PolicyResponse> Data, PageMeta Meta);
