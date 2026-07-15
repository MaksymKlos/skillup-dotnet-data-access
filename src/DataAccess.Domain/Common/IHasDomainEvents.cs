namespace DataAccess.Domain.Common;

/// <summary>
/// Non-generic view of an aggregate root's domain events. Infrastructure (e.g. the Outbox
/// interceptor) uses this to collect and clear events without knowing the concrete id type.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
