namespace Segfy.Api.Contracts;

public sealed record StatusHistoryEntryResponse(
    Guid Id,
    string FromStatus,
    string ToStatus,
    string? Reason,
    DateTime ChangedAt);

public sealed record StatusHistoryResponse(IReadOnlyList<StatusHistoryEntryResponse> Data);
