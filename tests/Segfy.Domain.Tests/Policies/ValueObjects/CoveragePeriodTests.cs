using FluentAssertions;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies.ValueObjects;
using Xunit;

namespace Segfy.Domain.Tests.Policies.ValueObjects;

public sealed class CoveragePeriodTests
{
    [Fact]
    public void Create_WithEndAfterStart_Succeeds()
    {
        var period = CoveragePeriod.Create(new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30));
        period.Start.Should().Be(new DateOnly(2026, 7, 1));
        period.End.Should().Be(new DateOnly(2027, 6, 30));
    }

    [Fact]
    public void Create_WithEndEqualStart_ThrowsDomainValidation()
    {
        var date = new DateOnly(2026, 7, 1);
        var act = () => CoveragePeriod.Create(date, date);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_WithEndBeforeStart_ThrowsDomainValidation()
    {
        var act = () => CoveragePeriod.Create(new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 1));
        act.Should().Throw<DomainValidationException>();
    }
}
