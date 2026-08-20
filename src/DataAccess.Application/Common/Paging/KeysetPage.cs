namespace DataAccess.Application.Common.Paging;

public sealed record KeysetPage<T>(
    IReadOnlyList<T> Items,
    DateTimeOffset? NextCreatedAt,
    Guid? NextId,
    bool HasMore);
