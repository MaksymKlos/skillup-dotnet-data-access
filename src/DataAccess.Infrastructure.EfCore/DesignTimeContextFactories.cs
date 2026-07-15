using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataAccess.Infrastructure.EfCore;

public sealed class PostgresAppDbContextFactory : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    public PostgresAppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__orders-postgres")
            ?? throw new InvalidOperationException("Connection string for PostgresAppDbContext is not set.");

        var options = new DbContextOptionsBuilder<PostgresAppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PostgresAppDbContext(options);
    }
}

public sealed class SqlServerAppDbContextFactory : IDesignTimeDbContextFactory<SqlServerAppDbContext>
{
    public SqlServerAppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__orders-sqlserver")
            ?? throw new InvalidOperationException("Connection string for SqlServerAppDbContext is not set.");

        var options = new DbContextOptionsBuilder<SqlServerAppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SqlServerAppDbContext(options);
    }
}
