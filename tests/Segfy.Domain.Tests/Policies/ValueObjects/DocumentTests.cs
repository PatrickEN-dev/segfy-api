using FluentAssertions;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies.ValueObjects;
using Xunit;

namespace Segfy.Domain.Tests.Policies.ValueObjects;

public sealed class DocumentTests
{
    [Fact]
    public void Create_WithValidCpf_ReturnsDigitsOnly()
    {
        var doc = Document.Create("52998224725");
        doc.Digits.Should().Be("52998224725");
    }

    [Fact]
    public void Create_WithValidCnpj_ReturnsDigitsOnly()
    {
        var doc = Document.Create("11222333000181");
        doc.Digits.Should().Be("11222333000181");
    }

    [Fact]
    public void Create_WithMask_StripsAndValidates()
    {
        var doc = Document.Create("529.982.247-25");
        doc.Digits.Should().Be("52998224725");
    }

    [Fact]
    public void Create_WithInvalidCheckDigits_ThrowsDomainValidation()
    {
        var act = () => Document.Create("52998224726");
        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999999")]
    public void Create_WithRepeatingDigits_ThrowsDomainValidation(string raw)
    {
        var act = () => Document.Create(raw);
        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678")]
    [InlineData("123456789012")]
    public void Create_WithWrongLength_ThrowsDomainValidation(string raw)
    {
        var act = () => Document.Create(raw);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void LoadTrusted_BypassesValidation()
    {
        var doc = Document.LoadTrusted("00000000000");
        doc.Digits.Should().Be("00000000000");
    }
}
