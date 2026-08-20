using DataAccess.Domain.Common;
using DataAccess.Domain.Orders.Identifiers;

namespace DataAccess.Domain.Orders.Events;

public sealed record OrderPlaced(
    OrderId OrderId,
    CustomerId CustomerId,
    decimal TotalAmount,
    string Currency) : IDomainEvent;
