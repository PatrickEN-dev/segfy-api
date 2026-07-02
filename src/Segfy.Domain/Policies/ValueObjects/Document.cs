using System.Text.RegularExpressions;
using Segfy.Domain.Common.Errors;

namespace Segfy.Domain.Policies.ValueObjects;

public sealed record Document
{
    private static readonly Regex NonDigits = new(@"[^\d]", RegexOptions.Compiled);

    public string Digits { get; }

    private Document(string digits)
    {
        Digits = digits;
    }

    public static Document Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainValidationException("Document is invalid.");

        var digits = NonDigits.Replace(raw, string.Empty);

        if (digits.Length != 11 && digits.Length != 14)
            throw new DomainValidationException("Document is invalid.");

        if (AllSameDigit(digits))
            throw new DomainValidationException("Document is invalid.");

        var valid = digits.Length == 11
            ? IsValidCpf(digits)
            : IsValidCnpj(digits);

        if (!valid)
            throw new DomainValidationException("Document is invalid.");

        return new Document(digits);
    }

    public static Document LoadTrusted(string digits) => new(digits);

    private static bool AllSameDigit(string digits)
    {
        var first = digits[0];
        for (var i = 1; i < digits.Length; i++)
            if (digits[i] != first) return false;
        return true;
    }

    private static bool IsValidCpf(string digits)
    {
        var d = new int[11];
        for (var i = 0; i < 11; i++) d[i] = digits[i] - '0';

        var sum1 = 0;
        for (var i = 0; i < 9; i++) sum1 += d[i] * (10 - i);
        var mod1 = sum1 % 11;
        var dv1 = mod1 < 2 ? 0 : 11 - mod1;
        if (dv1 != d[9]) return false;

        var sum2 = 0;
        for (var i = 0; i < 10; i++) sum2 += d[i] * (11 - i);
        var mod2 = sum2 % 11;
        var dv2 = mod2 < 2 ? 0 : 11 - mod2;
        return dv2 == d[10];
    }

    private static bool IsValidCnpj(string digits)
    {
        var d = new int[14];
        for (var i = 0; i < 14; i++) d[i] = digits[i] - '0';

        int[] w1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] w2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var sum1 = 0;
        for (var i = 0; i < 12; i++) sum1 += d[i] * w1[i];
        var mod1 = sum1 % 11;
        var dv1 = mod1 < 2 ? 0 : 11 - mod1;
        if (dv1 != d[12]) return false;

        var sum2 = 0;
        for (var i = 0; i < 13; i++) sum2 += d[i] * w2[i];
        var mod2 = sum2 % 11;
        var dv2 = mod2 < 2 ? 0 : 11 - mod2;
        return dv2 == d[13];
    }
}
