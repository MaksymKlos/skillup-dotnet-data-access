namespace DataAccess.Application.Orders.Queries;

public sealed record OrderLineDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
