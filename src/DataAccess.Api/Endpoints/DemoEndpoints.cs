using System.Data;
using DataAccess.Application.Products;
using DataAccess.Domain.Products.Identifiers;
using DataAccess.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Api.Endpoints;

public static class DemoEndpoints
{
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/demo").WithTags("Demo");

        // Optimistic concurrency: two independent contexts read the same product (same version
        // token), both decrement, both save. The first save wins; the second sees a changed
        // token and throws DbUpdateConcurrencyException — the lost update is detected, not silent.
        group.MapPost("/concurrency", async (
            ConcurrencyDemoRequest request,
            IServiceScopeFactory scopeFactory,
            CancellationToken ct) =>
        {
            var productId = new ProductId(request.ProductId);

            await using var scopeA = scopeFactory.CreateAsyncScope();
            await using var scopeB = scopeFactory.CreateAsyncScope();

            var contextA = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
            var contextB = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();

            var productA = await scopeA.ServiceProvider.GetRequiredService<IProductRepository>().GetAsync(productId, ct);
            var productB = await scopeB.ServiceProvider.GetRequiredService<IProductRepository>().GetAsync(productId, ct);
            if (productA is null || productB is null)
            {
                return Results.NotFound();
            }

            productA.Decrease(request.Quantity);
            productB.Decrease(request.Quantity);

            var first = await TrySaveAsync(contextA, ct);
            var second = await TrySaveAsync(contextB, ct);

            return Results.Ok(new { first, second });
        });

        // Read under a chosen isolation level, showing the transaction API (query ?level=Serializable).
        group.MapGet("/isolation", async (
            AppDbContext dbContext,
            CancellationToken ct,
            IsolationLevel level = IsolationLevel.ReadCommitted) =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(level, ct);
            var products = await dbContext.Products.CountAsync(ct);
            var orders = await dbContext.Orders.CountAsync(ct);
            await transaction.CommitAsync(ct);

            return Results.Ok(new { level = level.ToString(), products, orders });
        });

        return app;
    }

    private static async Task<string> TrySaveAsync(AppDbContext context, CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
            return "committed";
        }
        catch (DbUpdateConcurrencyException)
        {
            return "concurrency-conflict";
        }
    }

    public sealed record ConcurrencyDemoRequest(Guid ProductId, int Quantity);
}
