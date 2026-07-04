using DataAccess.Core.Domain.Common;
using DataAccess.Core.Domain.Orders.Identifiers;

namespace DataAccess.Core.Domain.Orders.Events;

/// <summary>Raised when a draft order is successfully placed.</summary>
public sealed record OrderPlaced(
    OrderId OrderId,
    CustomerId CustomerId,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset OccurredAt) : IDomainEvent;
