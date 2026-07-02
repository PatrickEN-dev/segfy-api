namespace Segfy.Api.Contracts;

public sealed record ExpiringMeta(int WindowDays, DateOnly Reference);
