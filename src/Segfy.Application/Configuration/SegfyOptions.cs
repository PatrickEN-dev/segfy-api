using System.ComponentModel.DataAnnotations;

namespace Segfy.Application.Configuration;

public sealed class SegfyOptions
{
    [Range(1, 365)]
    public int ExpiringWindowDays { get; init; } = 30;

    public bool AutoExpirationEnabled { get; init; } = true;

    [Range(60, 86_400)]
    public int AutoExpirationIntervalSeconds { get; init; } = 3_600;

    public bool SeedSampleData { get; init; } = true;
}
