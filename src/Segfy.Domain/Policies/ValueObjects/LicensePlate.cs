using System.Text.RegularExpressions;
using Segfy.Domain.Common.Errors;

namespace Segfy.Domain.Policies.ValueObjects;

public sealed record LicensePlate
{
    private static readonly Regex OldFormat = new(@"^[A-Z]{3}-?[0-9]{4}$", RegexOptions.Compiled);
    private static readonly Regex MercosulFormat = new(@"^[A-Z]{3}[0-9][A-Z][0-9]{2}$", RegexOptions.Compiled);

    public string Value { get; }

    private LicensePlate(string value)
    {
        Value = value;
    }

    public static LicensePlate Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainValidationException("License plate is invalid.");

        var normalized = raw.Trim().ToUpperInvariant().Replace(" ", string.Empty);

        if (OldFormat.IsMatch(normalized))
            return new LicensePlate(normalized.Replace("-", string.Empty));

        if (MercosulFormat.IsMatch(normalized))
            return new LicensePlate(normalized);

        throw new DomainValidationException("License plate is invalid.");
    }

    public static LicensePlate LoadTrusted(string value) => new(value);
}
