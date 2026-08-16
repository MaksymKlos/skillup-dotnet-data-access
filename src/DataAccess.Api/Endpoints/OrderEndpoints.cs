using DataAccess.Api.Http;
using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Application.Common.Paging;
using DataAccess.Application.Orders.Commands;
using DataAccess.Application.Orders.Queries;

namespace DataAccess.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");

        group.MapPost("/", async (
            PlaceOrderRequest request,
            ICommandHandler<PlaceOrderCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var command = new PlaceOrderCommand(
                request.CustomerId,
                request.Items.Select(item => new PlaceOrderItem(item.ProductId, item.Quantity)).ToArray());

            var result = await handler.HandleAsync(command, ct);
            return result.ToCreated(id => $"/orders/{id}");
        });

        group.MapGet("/{id:guid}", async (Guid id, IOrderQueries queries, CancellationToken ct) =>
        {
            var order = await queries.GetOrderDetailsAsync(id, ct);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        group.MapGet("/", async (
            IOrderQueries queries,
            CancellationToken ct,
            int pageSize = 20,
            DateTimeOffset? afterCreatedAt = null,
            Guid? afterId = null) =>
        {
            var page = await queries.ListOrderSummariesAsync(
                new KeysetPageRequest(pageSize, afterCreatedAt, afterId), ct);
            return Results.Ok(page);
        });

        return app;
    }

    public sealed record PlaceOrderRequest(Guid CustomerId, IReadOnlyList<PlaceOrderLine> Items);

    public sealed record PlaceOrderLine(Guid ProductId, int Quantity);
}
