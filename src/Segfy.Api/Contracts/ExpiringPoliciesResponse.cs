namespace Segfy.Api.Contracts;

public sealed record ExpiringPoliciesResponse(IReadOnlyList<PolicyResponse> Data, ExpiringMeta Meta);
