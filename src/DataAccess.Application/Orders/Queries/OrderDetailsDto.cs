namespace DataAccess.Application.Orders.Queries;

public sealed record OrderDetailsDto(
    Guid OrderId,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineDto> Lines);
