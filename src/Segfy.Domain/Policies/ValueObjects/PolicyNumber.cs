using System.Globalization;
using System.Text.RegularExpressions;
using Segfy.Domain.Common.Errors;

namespace Segfy.Domain.Policies.ValueObjects;

public sealed record PolicyNumber
{
    private static readonly Regex Format = new(@"^SEG-(\d{4})-(\d{4,})$", RegexOptions.Compiled);

    public string Value { get; }
    public int Year { get; }
    public int Sequential { get; }

    private PolicyNumber(int year, int sequential, string value)
    {
        Year = year;
        Sequential = sequential;
        Value = value;
    }

    public static PolicyNumber Create(int year, int sequential)
    {
        if (year is < 1900 or > 9999)
            throw new DomainValidationException("Policy number year is out of range.");
        if (sequential < 1)
            throw new DomainValidationException("Policy number sequential must be positive.");

        var value = $"SEG-{year:D4}-{sequential:D4}";
        return new PolicyNumber(year, sequential, value);
    }

    public static PolicyNumber Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException("Policy number is invalid.");

        var match = Format.Match(value);
        if (!match.Success)
            throw new DomainValidationException("Policy number is invalid.");

        var year = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var sequential = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return new PolicyNumber(year, sequential, value);
    }
}
