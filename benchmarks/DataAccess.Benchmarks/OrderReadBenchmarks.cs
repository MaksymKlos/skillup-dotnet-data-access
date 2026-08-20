using BenchmarkDotNet.Attributes;
using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Application.Orders.Commands;
using DataAccess.Application.Orders.Queries;
using DataAccess.Application.Products.Commands;
using DataAccess.Domain.Orders.Identifiers;
using DataAccess.Infrastructure.Dapper;
using DataAccess.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace DataAccess.Benchmarks;

// Reads one order-with-lines three ways against the same PostgreSQL instance, so the numbers reflect
// the mapping cost of each approach rather than different databases: Dapper (raw multi-mapping),
// EF Core tracking (change-tracker snapshots every entity), and EF Core no-tracking.
[MemoryDiagnoser]
public class OrderReadBenchmarks
{
    private const int LinesPerOrder = 20;

    private PostgreSqlContainer _container = null!;
    private ServiceProvider _services = null!;
    private Guid _orderId;

    [GlobalSetup]
    public async Task Setup()
    {
        _container = new PostgreSqlBuilder("postgres:17").Build();
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Postgres",
                ["ConnectionStrings:orders-postgres"] = _container.GetConnectionString(),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddEfCore(configuration);
        services.AddDapper(configuration);
        services.AddScoped<ICommandHandler<CreateProductCommand, Result<Guid>>, CreateProductCommandHandler>();
        services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<Guid>>, PlaceOrderCommandHandler>();
        _services = services.BuildServiceProvider();

        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

        _orderId = await SeedOrderAsync();
    }

    private async Task<Guid> SeedOrderAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var createProduct = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateProductCommand, Result<Guid>>>();
        var placeOrder = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<PlaceOrderCommand, Result<Guid>>>();

        var items = new List<PlaceOrderItem>(LinesPerOrder);
        for (var i = 0; i < LinesPerOrder; i++)
        {
            var created = await createProduct.HandleAsync(new CreateProductCommand($"BENCH-SKU-{i}", 9.99m, "USD", 1000));
            items.Add(new PlaceOrderItem(created.Value, 1));
        }

        var placed = await placeOrder.HandleAsync(new PlaceOrderCommand(Guid.NewGuid(), items));
        return placed.Value;
    }

    [Benchmark(Baseline = true)]
    public async Task<int> Dapper()
    {
        await using var scope = _services.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<IOrderQueries>();
        var details = await queries.GetOrderDetailsAsync(_orderId);
        return details!.Lines.Count;
    }

    [Benchmark]
    public async Task<int> EfCore_Tracking()
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await dbContext.Orders.FirstAsync(o => o.Id == new OrderId(_orderId));
        return order.Lines.Count;
    }

    [Benchmark]
    public async Task<int> EfCore_NoTracking()
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await dbContext.Orders.AsNoTracking().FirstAsync(o => o.Id == new OrderId(_orderId));
        return order.Lines.Count;
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _services.DisposeAsync();
        await _container.DisposeAsync();
    }
}
