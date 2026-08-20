using DataAccess.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Infrastructure.EfCore.Configurations;

internal static class MoneyOwnedConfiguration
{
    // Money is mapped the same way wherever it is owned (Order line unit price, Product price):
    // both providers must agree on numeric(18,2) / decimal(18,2) and a 3-char currency code.
    public static void ConfigureMoney<TOwner>(this OwnedNavigationBuilder<TOwner, Money> money)
        where TOwner : class
    {
        money.Property(m => m.Amount).HasPrecision(18, 2);
        money.Property(m => m.Currency).HasMaxLength(3);
    }
}
