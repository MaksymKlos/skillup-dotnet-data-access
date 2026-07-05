using DataAccess.Domain.Common;
using Shouldly;
using Xunit;

namespace DataAccess.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Same_amount_and_currency_are_equal_by_value()
    {
        new Money(10m, "USD").ShouldBe(new Money(10m, "USD"));
    }

    [Fact]
    public void Currency_is_normalised_to_upper_case()
    {
        new Money(1m, "usd").Currency.ShouldBe("USD");
    }

    [Fact]
    public void Add_sums_amounts_of_the_same_currency()
    {
        var result = new Money(10m, "USD").Add(new Money(5m, "USD"));

        result.Amount.ShouldBe(15m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Add_of_different_currencies_throws()
    {
        Should.Throw<DomainException>(() => new Money(10m, "USD").Add(new Money(5m, "EUR")));
    }

    [Fact]
    public void Negative_amount_throws()
    {
        Should.Throw<DomainException>(() => new Money(-10m, "USD"));
    }
}
