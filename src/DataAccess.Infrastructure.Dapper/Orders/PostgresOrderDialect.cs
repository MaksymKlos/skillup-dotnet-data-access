namespace DataAccess.Infrastructure.Dapper.Orders;

// EF created PascalCase columns, which Postgres treats as case-sensitive, hence the quoting.
public sealed class PostgresOrderDialect : OrderDialect
{
    public override string OrderDetailsSql => """
        SELECT
            o."Id", o."CustomerId", o."Status", o."CreatedAt",
            l."Id" AS LineId, l."ProductId", l."Quantity",
            l."UnitPrice_Amount" AS UnitPrice, l."UnitPrice_Currency" AS Currency
        FROM orders o
        LEFT JOIN order_lines l ON l."OrderId" = o."Id"
        WHERE o."Id" = @orderId
        ORDER BY l."Id";
        """;

    public override string BuildOrderSummariesSql(bool hasCursor)
    {
        // Postgres supports row-value comparison for the keyset predicate.
        var keyset = hasCursor
            ? """WHERE (o."CreatedAt", o."Id") > (@afterCreatedAt, @afterId)"""
            : string.Empty;

        return $"""
            SELECT
                o."Id" AS OrderId, o."CustomerId" AS CustomerId, o."Status" AS Status, o."CreatedAt" AS CreatedAt,
                COALESCE(SUM(l."UnitPrice_Amount" * l."Quantity"), 0) AS TotalAmount,
                COALESCE(MAX(l."UnitPrice_Currency"), '') AS Currency,
                COUNT(l."Id") AS LineCount
            FROM orders o
            LEFT JOIN order_lines l ON l."OrderId" = o."Id"
            {keyset}
            GROUP BY o."Id", o."CustomerId", o."Status", o."CreatedAt"
            ORDER BY o."CreatedAt", o."Id"
            LIMIT @limit;
            """;
    }
}
