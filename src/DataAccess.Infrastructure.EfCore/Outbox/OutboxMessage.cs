namespace DataAccess.Infrastructure.EfCore.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    public string Type { get; init; } = null!;

    public string Content { get; init; } = null!;

    public DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }
}
