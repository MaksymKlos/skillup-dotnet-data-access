namespace DataAccess.Application.Common.Paging;

/// <summary>
/// A page of results plus the cursor to fetch the next page.
/// </summary>
public sealed record KeysetPage<T>(
    IReadOnlyList<T> Items,
    DateTimeOffset? NextCreatedAt,
    Guid? NextId,
    bool HasMore);
