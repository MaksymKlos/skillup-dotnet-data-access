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
        var connectionString = configuration.GetConnectionString(DatabaseConnectionNames.Postgres);
        var enableRetry = IsRetryEnabled(configuration);

        services.AddDbContext<PostgresAppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                if (enableRetry)
                {
                    npgsql.EnableRetryOnFailure();
                }
            });
            options.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
        });

        services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<PostgresAppDbContext>());
    }

    private static void RegisterSqlServer(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseConnectionNames.SqlServer);
        var enableRetry = IsRetryEnabled(configuration);

        services.AddDbContext<SqlServerAppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                if (enableRetry)
                {
                    sql.EnableRetryOnFailure();
                }
            });
            options.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
        });

        services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<SqlServerAppDbContext>());
    }

    // Off by default: a retrying execution strategy forbids user-initiated transactions, which the
    // concurrency/isolation demos rely on. Turn it on only where explicit transactions are wrapped
    // in db.Database.CreateExecutionStrategy().ExecuteAsync(...).
    private static bool IsRetryEnabled(IConfiguration configuration)
        => bool.TryParse(configuration["Database:EnableRetryOnFailure"], out var enabled) && enabled;
}
