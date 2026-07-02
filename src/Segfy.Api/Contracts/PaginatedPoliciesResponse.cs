namespace Segfy.Api.Contracts;

public sealed record PaginatedPoliciesResponse(IReadOnlyList<PolicyResponse> Data, PageMeta Meta);
