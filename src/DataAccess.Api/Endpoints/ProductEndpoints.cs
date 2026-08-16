using DataAccess.Api.Http;
using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Application.Products.Commands;

namespace DataAccess.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Products");

        group.MapPost("/", async (
            CreateProductRequest request,
            ICommandHandler<CreateProductCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(
                new CreateProductCommand(request.Sku, request.Price, request.Currency, request.InitialStock), ct);
            return result.ToCreated(id => $"/products/{id}");
        });

        group.MapPost("/{id:guid}/restock", async (
            Guid id,
            RestockRequest request,
            ICommandHandler<RestockProductCommand, Result> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new RestockProductCommand(id, request.Quantity), ct);
            return result.ToNoContent();
        });

        return app;
    }

    public sealed record CreateProductRequest(string Sku, decimal Price, string Currency, int InitialStock);

    public sealed record RestockRequest(int Quantity);
}
