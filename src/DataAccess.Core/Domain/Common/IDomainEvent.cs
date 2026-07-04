namespace DataAccess.Core.Domain.Common;

/// <summary>
/// Marker for something meaningful that happened in the domain
/// (e.g. an order was placed). Raised by aggregate roots.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
