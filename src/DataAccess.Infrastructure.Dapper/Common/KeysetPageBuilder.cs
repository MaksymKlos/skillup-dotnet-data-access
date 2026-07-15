using DataAccess.Application.Common.Paging;

namespace DataAccess.Infrastructure.Dapper.Common;

public static class KeysetPageBuilder
{
    // Callers fetch pageSize + 1 rows; the extra row means there is a next page. The cursor
    // is the sort key of the last returned item, and only meaningful when there is more.
    public static KeysetPage<T> Build<T>(
        IReadOnlyList<T> rows,
        int pageSize,
        Func<T, DateTimeOffset> createdAtSelector,
        Func<T, Guid> idSelector)
    {
        var hasMore = rows.Count > pageSize;
        var items = (hasMore ? rows.Take(pageSize) : rows).ToArray();

        if (!hasMore || items.Length == 0)
        {
            return new KeysetPage<T>(items, null, null, hasMore);
        }

        var last = items[^1];
        return new KeysetPage<T>(items, createdAtSelector(last), idSelector(last), true);
    }
}
