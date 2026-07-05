namespace DataAccess.Application.Orders.Queries;

/// <summary>Flat read model for an order in a list. Primitives only (no typed ids).</summary>
public sealed record OrderSummaryDto(
    Guid OrderId,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    int LineCount,
    DateTimeOffset CreatedAt);
