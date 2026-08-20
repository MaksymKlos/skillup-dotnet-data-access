namespace DataAccess.Infrastructure.EfCore;

// Aspire resource / connection-string names. Must stay in sync with AppHost.AddDatabase(...).
public static class DatabaseConnectionNames
{
    public const string Postgres = "orders-postgres";
    public const string SqlServer = "orders-sqlserver";
}
