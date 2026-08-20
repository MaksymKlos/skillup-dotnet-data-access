namespace DataAccess.Infrastructure.Dapper.Products;

// Pessimistic locking is expressed differently per provider: Postgres uses row-level SELECT ... FOR
// UPDATE, SQL Server uses the UPDLOCK/ROWLOCK table hints. Both hold the row until the surrounding
// transaction commits, so concurrent writers serialize instead of racing.
public abstract class ProductLockDialect
{
    public abstract string LockAndReadStockSql { get; }

    public abstract string DecrementStockSql { get; }
}

public sealed class PostgresProductLockDialect : ProductLockDialect
{
    public override string LockAndReadStockSql =>
        """SELECT "Stock" FROM products WHERE "Id" = @id FOR UPDATE;""";

    public override string DecrementStockSql =>
        """UPDATE products SET "Stock" = "Stock" - @quantity WHERE "Id" = @id;""";
}

public sealed class SqlServerProductLockDialect : ProductLockDialect
{
    public override string LockAndReadStockSql =>
        "SELECT Stock FROM products WITH (UPDLOCK, ROWLOCK) WHERE Id = @id;";

    public override string DecrementStockSql =>
        "UPDATE products SET Stock = Stock - @quantity WHERE Id = @id;";
}
