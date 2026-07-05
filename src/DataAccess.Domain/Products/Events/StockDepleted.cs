using DataAccess.Domain.Common;
using DataAccess.Domain.Products.Identifiers;

namespace DataAccess.Domain.Products.Events;

public sealed record StockDepleted(
    ProductId ProductId,
    string Sku,
    DateTimeOffset OccurredAt) : IDomainEvent;
