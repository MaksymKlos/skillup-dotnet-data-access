namespace DataAccess.Application.Orders.Queries;

public sealed record OrderSummaryDto(
    Guid OrderId,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    int LineCount,
    DateTimeOffset CreatedAt);
