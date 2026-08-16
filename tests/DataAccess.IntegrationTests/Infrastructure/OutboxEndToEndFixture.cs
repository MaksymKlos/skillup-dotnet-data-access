using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Application.Orders.Commands;
using DataAccess.Application.Products.Commands;
using DataAccess.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace DataAccess.IntegrationTests.Infrastructure;

// The end-to-end Outbox test needs both a database (to capture events) and a broker (to relay them),
// so this fixture starts both containers and exposes a live RabbitMQ connection.
public sealed class OutboxEndToEndFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management").Build();

    public ServiceProvider Services { get; private set; } = default!;

    public IConnection Connection { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Postgres",
                ["ConnectionStrings:orders-postgres"] = _postgres.GetConnectionString(),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddEfCore(configuration);
        services.AddScoped<ICommandHandler<CreateProductCommand, Result<Guid>>, CreateProductCommandHandler>();
        services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<Guid>>, PlaceOrderCommandHandler>();
        Services = services.BuildServiceProvider();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

        var connectionFactory = new ConnectionFactory { Uri = new Uri(_rabbitMq.GetConnectionString()) };
        Connection = await connectionFactory.CreateConnectionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        await Services.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
