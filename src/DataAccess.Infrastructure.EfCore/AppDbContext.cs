using DataAccess.Application.Abstractions;
using DataAccess.Domain.Common;
using DataAccess.Domain.Orders;
using DataAccess.Domain.Products;
using DataAccess.Infrastructure.EfCore.Conversions;
using DataAccess.Infrastructure.EfCore.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Infrastructure.EfCore;

public abstract class AppDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private static readonly Type[] StronglyTypedIdTypes = typeof(IStronglyTypedId).Assembly
        .GetTypes()
        .Where(type => type is { IsValueType: true } && typeof(IStronglyTypedId).IsAssignableFrom(type))
        .ToArray();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        foreach (var idType in StronglyTypedIdTypes)
        {
            configurationBuilder
                .Properties(idType)
                .HaveConversion(typeof(StronglyTypedIdConverter<>).MakeGenericType(idType));
        }
    }
}
