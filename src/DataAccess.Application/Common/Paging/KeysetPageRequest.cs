namespace DataAccess.Application.Common.Paging;

/// <summary>
/// Cursor-based paging request. The cursor is the sort key of the last row seen
/// (created-at + id tie-breaker); null cursor means the first page.
/// </summary>
public sealed record KeysetPageRequest(
    int PageSize,
    DateTimeOffset? AfterCreatedAt = null,
    Guid? AfterId = null);
