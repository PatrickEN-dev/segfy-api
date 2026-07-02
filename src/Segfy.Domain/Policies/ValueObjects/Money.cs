using Segfy.Domain.Common.Errors;

namespace Segfy.Domain.Policies.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public static Money Create(decimal amount)
    {
        if (amount <= 0m)
            throw new DomainValidationException("Premium must be greater than zero.");

        if (decimal.Round(amount, 2) != amount)
            throw new DomainValidationException("Premium can have at most 2 decimal places.");

        return new Money(amount);
    }

    public static Money LoadTrusted(decimal amount) => new(amount);
}
