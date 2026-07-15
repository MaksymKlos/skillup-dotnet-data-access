using DataAccess.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Infrastructure.EfCore.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.Property(o => o.Status).HasConversion<int>();

        builder.OwnsMany(o => o.Lines, lines =>
        {
            lines.ToTable("order_lines");
            lines.HasKey(l => l.Id);

            lines.OwnsOne(l => l.UnitPrice, price =>
            {
                price.Property(p => p.Amount).HasPrecision(18, 2);
                price.Property(p => p.Currency).HasMaxLength(3);
            });
            lines.Navigation(l => l.UnitPrice).IsRequired();

            lines.Ignore(l => l.LineTotal);
        });

        // Lines are exposed as a read-only view, so EF must go through the _lines field.
        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.DomainEvents);
    }
}
