using FluentAssertions;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies.ValueObjects;
using Xunit;

namespace Segfy.Domain.Tests.Policies.ValueObjects;

public sealed class PolicyNumberTests
{
    [Fact]
    public void Create_ForYear2026Seq1_ReturnsSEG_2026_0001()
    {
        var pn = PolicyNumber.Create(2026, 1);
        pn.Value.Should().Be("SEG-2026-0001");
        pn.Year.Should().Be(2026);
        pn.Sequential.Should().Be(1);
    }

    [Fact]
    public void Create_ForYear2026Seq9999_ReturnsSEG_2026_9999()
    {
        var pn = PolicyNumber.Create(2026, 9999);
        pn.Value.Should().Be("SEG-2026-9999");
    }

    [Fact]
    public void Create_ForYear2026Seq10000_ExpandsPadding()
    {
        var pn = PolicyNumber.Create(2026, 10000);
        pn.Value.Should().Be("SEG-2026-10000");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithZeroOrNegativeSequential_ThrowsDomainValidation(int seq)
    {
        var act = () => PolicyNumber.Create(2026, seq);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Parse_ValidValue_RoundTrips()
    {
        var pn = PolicyNumber.Parse("SEG-2026-0042");
        pn.Year.Should().Be(2026);
        pn.Sequential.Should().Be(42);
        pn.Value.Should().Be("SEG-2026-0042");
    }

    [Theory]
    [InlineData("")]
    [InlineData("SEG-26-0001")]
    [InlineData("SEG-2026-1")]
    [InlineData("ABC-2026-0001")]
    public void Parse_InvalidValue_ThrowsDomainValidation(string raw)
    {
        var act = () => PolicyNumber.Parse(raw);
        act.Should().Throw<DomainValidationException>();
    }
}
