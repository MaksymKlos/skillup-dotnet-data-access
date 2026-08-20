namespace DataAccess.Infrastructure.EfCore.Inbox;

// Consumer-side idempotency record. A processed message id is stored here so a redelivered
// message (at-least-once transport) is recognised and skipped instead of handled twice.
public sealed class InboxMessage
{
    public string MessageId { get; init; } = null!;

    public DateTimeOffset ProcessedAt { get; init; }
}
