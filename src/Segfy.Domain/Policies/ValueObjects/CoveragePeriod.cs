using Segfy.Domain.Common.Errors;

namespace Segfy.Domain.Policies.ValueObjects;

public sealed record CoveragePeriod
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    private CoveragePeriod(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public static CoveragePeriod Create(DateOnly start, DateOnly end)
    {
        if (end <= start)
            throw new DomainValidationException("CoverageEnd must be greater than CoverageStart.");
        return new CoveragePeriod(start, end);
    }

    public static CoveragePeriod LoadTrusted(DateOnly start, DateOnly end) => new(start, end);
}
