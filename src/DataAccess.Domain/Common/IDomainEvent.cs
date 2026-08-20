namespace DataAccess.Domain.Common;

// Marker for domain events. The timestamp is stamped by the Outbox at capture time,
// so the domain never reads the clock.
public interface IDomainEvent;
