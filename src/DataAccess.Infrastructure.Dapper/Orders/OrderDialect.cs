namespace DataAccess.Infrastructure.Dapper.Orders;

// Read-side SQL differs between providers: identifier quoting, keyset predicate (row-value
// comparison vs expanded OR), and the row-limit clause. Each provider supplies its own.
public abstract class OrderDialect
{
    public abstract string OrderDetailsSql { get; }

    public abstract string BuildOrderSummariesSql(bool hasCursor);
}
