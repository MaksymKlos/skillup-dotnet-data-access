using DataAccess.Application.Orders.Queries;
using DataAccess.Application.Products;
using DataAccess.Domain.Products.Identifiers;
using DataAccess.Infrastructure.EfCore;
using DataAccess.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DataAccess.IntegrationTests;

// Subset that proves the SQL Server provider works: migrations apply, an EF write round-trips through
// the Dapper read side, and the rowversion token detects concurrent updates.
public sealed class SqlServerTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    private readonly IServiceProvider _services = fixture.Services;

    [Fact]
    public async Task Places_order_and_reads_it_back()
    {
        var ct = TestContext.Current.CancellationToken;
        var product = await TestData.CreateProductAsync(_services, stock: 8, ct, price: 4m);

        var orderId = await TestData.PlaceOrderAsync(_services, [(product, 2)], ct);

        await using var scope = _services.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<IOrderQueries>();
        var details = await queries.GetOrderDetailsAsync(orderId, ct);

        details.ShouldNotBeNull();
        details.Lines.Count.ShouldBe(1);
        details.TotalAmount.ShouldBe(4m * 2);
    }

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
}
