using DataAccess.Domain.Products;
using DataAccess.Infrastructure.EfCore.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Infrastructure.EfCore;

public sealed class PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options)
    : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map a uint shadow property to Postgres' system "xmin" column for optimistic concurrency.
        modelBuilder.Entity<Product>().Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
        modelBuilder.Entity<OutboxMessage>().Property(m => m.Content).HasColumnType("jsonb");
    }
}
