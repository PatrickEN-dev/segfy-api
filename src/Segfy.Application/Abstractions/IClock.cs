namespace Segfy.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly TodayUtc { get; }
}
