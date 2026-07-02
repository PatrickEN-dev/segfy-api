using FluentAssertions;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies.ValueObjects;
using Xunit;

namespace Segfy.Domain.Tests.Policies.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Create_WithPositive_Succeeds()
    {
        var money = Money.Create(199.90m);
        money.Amount.Should().Be(199.90m);
    }

    [Fact]
    public void Create_WithZero_ThrowsDomainValidation()
    {
        var act = () => Money.Create(0m);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_WithNegative_ThrowsDomainValidation()
    {
        var act = () => Money.Create(-1m);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_WithThreeDecimals_ThrowsDomainValidation()
    {
        var act = () => Money.Create(199.905m);
        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData(199.9)]
    [InlineData(200)]
    [InlineData(1)]
    public void Create_WithUpToTwoDecimals_Succeeds(double value)
    {
        var money = Money.Create((decimal)value);
        money.Amount.Should().BeGreaterThan(0);
    }
}
