using DataAccess.Application.Abstractions;
using DataAccess.Application.Orders;
using DataAccess.Application.Products;
using DataAccess.Infrastructure.EfCore.Outbox;
using DataAccess.Infrastructure.EfCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.Infrastructure.EfCore;

public static class DependencyInjection
{
    private const string PostgresConnectionName = "ordersdb";
    private const string SqlServerConnectionName = "OrdersDb";

    public static IServiceCollection AddEfCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<OutboxInterceptor>();

        var provider = configuration["Database:Provider"] ?? "Postgres";

        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            RegisterSqlServer(services, configuration);
        }
        else
        {
            RegisterPostgres(services, configuration);
        }

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IProductRepository, EfProductRepository>();

        return services;
    }

    private static void RegisterPostgres(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(PostgresConnectionName);

        services.AddDbContext<PostgresAppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
        });

        services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<PostgresAppDbContext>());
    }

    private static void RegisterSqlServer(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(SqlServerConnectionName);

        services.AddDbContext<SqlServerAppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            options.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
        });

        services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<SqlServerAppDbContext>());
    }
}
