using DataAccess.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Infrastructure.EfCore.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.Property(p => p.Sku).HasMaxLength(100);

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount);
            price.Property(m => m.Currency).HasMaxLength(3);
        });
        builder.Navigation(p => p.Price).IsRequired();

        builder.Ignore(p => p.DomainEvents);
    }
}
