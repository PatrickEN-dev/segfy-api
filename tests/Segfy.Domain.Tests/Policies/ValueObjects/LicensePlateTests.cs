using FluentAssertions;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies.ValueObjects;
using Xunit;

namespace Segfy.Domain.Tests.Policies.ValueObjects;

public sealed class LicensePlateTests
{
    [Fact]
    public void Create_WithOldFormatDashed_NormalizesToNoDash()
    {
        var plate = LicensePlate.Create("ABC-1234");
        plate.Value.Should().Be("ABC1234");
    }

    [Fact]
    public void Create_WithOldFormatNoDash_KeepsAsIs()
    {
        var plate = LicensePlate.Create("ABC1234");
        plate.Value.Should().Be("ABC1234");
    }

    [Fact]
    public void Create_WithMercosulFormat_KeepsAsIs()
    {
        var plate = LicensePlate.Create("ABC1D23");
        plate.Value.Should().Be("ABC1D23");
    }

    [Fact]
    public void Create_WithLowercase_Uppercases()
    {
        var plate = LicensePlate.Create("abc1d23");
        plate.Value.Should().Be("ABC1D23");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("ABCD123")]
    [InlineData("AB-1234")]
    [InlineData("AAAA-9999")]
    public void Create_WithInvalidFormat_ThrowsDomainValidation(string raw)
    {
        var act = () => LicensePlate.Create(raw);
        act.Should().Throw<DomainValidationException>();
    }
}
