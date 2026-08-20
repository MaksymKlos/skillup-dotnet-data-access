using System.Data;
using DataAccess.Application.Products;
using DataAccess.Domain.Common;
using DataAccess.Domain.Products;
using DataAccess.Domain.Products.Identifiers;
using DataAccess.Infrastructure.EfCore;
using DataAccess.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DataAccess.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PostgresBehaviorTests(PostgresFixture fixture)
{
    private readonly IServiceProvider _services = fixture.Services;

    [Fact]
    public async Task Concurrent_stock_decrement_is_detected_as_a_conflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = new ProductId(await TestData.CreateProductAsync(_services, stock: 10, ct));

        await using var scopeA = _services.CreateAsyncScope();
        await using var scopeB = _services.CreateAsyncScope();

        var contextA = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();
        var productA = await scopeA.ServiceProvider.GetRequiredService<IProductRepository>().GetAsync(productId, ct);
        var productB = await scopeB.ServiceProvider.GetRequiredService<IProductRepository>().GetAsync(productId, ct);

        productA!.Decrease(1);
        productB!.Decrease(1);

        await contextA.SaveChangesAsync(ct);
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task Rolled_back_transaction_persists_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = ProductId.New();

        await using (var scope = _services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            repository.Add(Product.Create(productId, "SKU-ROLLBACK", new Money(5m, "USD"), 3));
            await context.SaveChangesAsync(ct);
            await transaction.RollbackAsync(ct);
        }

        await using (var scope = _services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            (await repository.GetAsync(productId, ct)).ShouldBeNull();
        }
    }

    [Fact]
    public async Task Placing_an_order_captures_an_outbox_message()
    {
        var ct = TestContext.Current.CancellationToken;
        var product = await TestData.CreateProductAsync(_services, stock: 5, ct);
        var orderId = await TestData.PlaceOrderAsync(_services, [(product, 1)], ct);

        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var placedEvents = await context.OutboxMessages.AsNoTracking()
            .Where(message => message.Type == "OrderPlaced")
            .ToListAsync(ct);

        placedEvents.ShouldContain(message => message.Content.Contains(orderId.ToString()));
    }

    [Fact]
    public async Task Reads_under_serializable_isolation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var products = await context.Products.CountAsync(ct);
        await transaction.CommitAsync(ct);

        products.ShouldBeGreaterThanOrEqualTo(0);
    }
}
