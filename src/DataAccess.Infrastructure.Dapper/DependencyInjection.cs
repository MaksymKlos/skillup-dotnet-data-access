using DataAccess.Application.Orders.Queries;
using DataAccess.Infrastructure.Dapper.Orders;
using DataAccess.Infrastructure.Dapper.Products;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.Infrastructure.Dapper;

public static class DependencyInjection
{
    private const string PostgresConnectionName = "orders-postgres";
    private const string SqlServerConnectionName = "orders-sqlserver";

    public static IServiceCollection AddDapper(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Postgres";

        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ISqlConnectionFactory>(
                new SqlServerConnectionFactory(GetConnectionString(configuration, SqlServerConnectionName)));
            services.AddSingleton<OrderDialect, SqlServerOrderDialect>();
            services.AddSingleton<ProductLockDialect, SqlServerProductLockDialect>();
        }
        else
        {
            services.AddSingleton<ISqlConnectionFactory>(
                new PostgresConnectionFactory(GetConnectionString(configuration, PostgresConnectionName)));
            services.AddSingleton<OrderDialect, PostgresOrderDialect>();
            services.AddSingleton<ProductLockDialect, PostgresProductLockDialect>();
        }

        services.AddScoped<IOrderQueries, DapperOrderQueries>();

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration, string name)
        => configuration.GetConnectionString(name)
           ?? throw new InvalidOperationException($"Connection string '{name}' is not configured.");
}
