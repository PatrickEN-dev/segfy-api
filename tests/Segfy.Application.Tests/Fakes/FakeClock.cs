using Segfy.Application.Abstractions;

namespace Segfy.Application.Tests.Fakes;

public sealed class FakeClock : IClock
{
    private DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow => _now;
    public DateOnly TodayUtc => DateOnly.FromDateTime(_now);

    public void SetNow(DateTime nowUtc) => _now = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
}
