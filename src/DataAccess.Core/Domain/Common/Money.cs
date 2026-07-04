namespace DataAccess.Core.Domain.Common;

/// <summary>
/// Money value object: an amount plus an ISO currency code. Immutable, compared
/// by value. Operations across different currencies are rejected.
/// </summary>
public sealed class Money : ValueObject
{
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int factor)
    {
        if (factor < 0)
        {
            throw new DomainException("Multiplication factor cannot be negative.");
        }

        return new Money(Amount * factor, Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException(
                $"Cannot operate on different currencies: {Currency} and {other.Currency}.");
        }
    }
}
