namespace DataAccess.Application.Orders.Queries;

/// <summary>Flat read model for a single order with its lines.</summary>
public sealed record OrderDetailsDto(
    Guid OrderId,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineDto> Lines);
