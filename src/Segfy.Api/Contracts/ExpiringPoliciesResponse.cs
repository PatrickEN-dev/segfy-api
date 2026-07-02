namespace Segfy.Api.Contracts;

public sealed record ExpiringMeta(int WindowDays, DateOnly Reference);

public sealed record ExpiringPoliciesResponse(IReadOnlyList<PolicyResponse> Data, ExpiringMeta Meta);
