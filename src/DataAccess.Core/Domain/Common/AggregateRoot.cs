namespace DataAccess.Core.Domain.Common;

/// <summary>
/// Base class for aggregate roots — the single entry point into an aggregate and
/// the boundary of a transaction. Only the root records domain events.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }

    protected AggregateRoot() { }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
