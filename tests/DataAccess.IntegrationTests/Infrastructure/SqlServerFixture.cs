using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Application.Orders.Commands;
using DataAccess.Application.Products.Commands;
using DataAccess.Infrastructure.Dapper;
using DataAccess.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace DataAccess.IntegrationTests.Infrastructure;

// SQL Server counterpart of PostgresFixture. Used by a smaller subset of tests to prove the second
// provider works end to end (migrations, a round-trip, rowversion concurrency).
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public ServiceProvider Services { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "SqlServer",
                ["ConnectionStrings:orders-sqlserver"] = _container.GetConnectionString(),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddEfCore(configuration);
        services.AddDapper(configuration);
        services.AddScoped<ICommandHandler<CreateProductCommand, Result<Guid>>, CreateProductCommandHandler>();
        services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<Guid>>, PlaceOrderCommandHandler>();
        Services = services.BuildServiceProvider();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        await _container.DisposeAsync();
    }
}
