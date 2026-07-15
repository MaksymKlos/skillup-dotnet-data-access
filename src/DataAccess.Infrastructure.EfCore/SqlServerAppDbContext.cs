using DataAccess.Domain.Products;
using DataAccess.Infrastructure.EfCore.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Infrastructure.EfCore;

public sealed class SqlServerAppDbContext(DbContextOptions<SqlServerAppDbContext> options)
    : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().Property<byte[]>("RowVersion").IsRowVersion();
        modelBuilder.Entity<OutboxMessage>().Property(m => m.Content).HasColumnType("nvarchar(max)");
    }
}
