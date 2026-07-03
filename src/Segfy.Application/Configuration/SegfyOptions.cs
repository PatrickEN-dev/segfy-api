using System.ComponentModel.DataAnnotations;

namespace Segfy.Application.Configuration;

public sealed class SegfyOptions
{
    [Range(1, 365)]
    public int ExpiringWindowDays { get; init; } = 30;

    public bool AutoExpirationEnabled { get; init; } = true;

    [Range(60, 86_400)]
    public int AutoExpirationIntervalSeconds { get; init; } = 3_600;

    // When true, the app populates 6 sample policies on boot if the DB is empty.
    // Set to false in tests and in production data that must not be seeded.
    public bool SeedSampleData { get; init; } = true;
}
