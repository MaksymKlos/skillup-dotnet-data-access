using DataAccess.Application.Common.Paging;
using DataAccess.Application.Orders.Queries;
using DataAccess.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DataAccess.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PostgresPersistenceTests(PostgresFixture fixture)
{
    private readonly IServiceProvider _services = fixture.Services;

    [Fact]
    public async Task Places_order_with_ef_and_reads_it_back_with_dapper()
    {
        var ct = TestContext.Current.CancellationToken;
        var first = await TestData.CreateProductAsync(_services, stock: 10, ct, price: 5m);
        var second = await TestData.CreateProductAsync(_services, stock: 10, ct, price: 7m);

        var orderId = await TestData.PlaceOrderAsync(_services, [(first, 2), (second, 3)], ct);

        await using var scope = _services.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<IOrderQueries>();
        var details = await queries.GetOrderDetailsAsync(orderId, ct);

        details.ShouldNotBeNull();
        details.OrderId.ShouldBe(orderId);
        details.Status.ShouldBe("Placed");
        details.Currency.ShouldBe("USD");
        details.Lines.Count.ShouldBe(2);
        details.TotalAmount.ShouldBe(5m * 2 + 7m * 3);
    }

    [Fact]
    public async Task Keyset_paging_covers_every_row_without_duplicates()
    {
        var ct = TestContext.Current.CancellationToken;
        var product = await TestData.CreateProductAsync(_services, stock: 1000, ct);

        var seeded = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            seeded.Add(await TestData.PlaceOrderAsync(_services, [(product, 1)], ct));
        }

        await using var scope = _services.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<IOrderQueries>();

        var seen = new List<Guid>();
        DateTimeOffset? afterCreatedAt = null;
        Guid? afterId = null;
        while (true)
        {
            var page = await queries.ListOrderSummariesAsync(
                new KeysetPageRequest(2, afterCreatedAt, afterId), ct);

            seen.AddRange(page.Items.Select(item => item.OrderId));
            if (!page.HasMore)
            {
                break;
            }

            afterCreatedAt = page.NextCreatedAt;
            afterId = page.NextId;
        }

        seen.ShouldBeUnique();
        foreach (var id in seeded)
        {
            seen.ShouldContain(id);
        }
    }
}
