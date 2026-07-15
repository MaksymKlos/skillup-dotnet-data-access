using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace DataAccess.Infrastructure.Dapper;

public interface ISqlConnectionFactory
{
    DbConnection Create();
}

public sealed class PostgresConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public DbConnection Create() => new NpgsqlConnection(connectionString);
}

public sealed class SqlServerConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public DbConnection Create() => new SqlConnection(connectionString);
}
