using DataAccess.Core.Domain.Common;
using DataAccess.Core.Domain.Products.Identifiers;

namespace DataAccess.Core.Domain.Products.Events;

/// <summary>Raised when an inventory item's stock reaches zero.</summary>
public sealed record StockDepleted(
    ProductId ProductId,
    string Sku,
    DateTimeOffset OccurredAt) : IDomainEvent;
