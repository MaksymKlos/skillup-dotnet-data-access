using System.Data;
using Dapper;
using DataAccess.Application.Products;
using DataAccess.Domain.Products.Identifiers;
using DataAccess.Infrastructure.Dapper;
using DataAccess.Infrastructure.Dapper.Products;
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

        // Pessimistic locking: each writer opens its own transaction and locks the product row before
        // reading stock (SELECT ... FOR UPDATE / UPDLOCK). The second writer blocks until the first
        // commits, so the two decrements serialize and no update is lost — the opposite trade-off to
        // the optimistic demo above, which lets both proceed and rejects the loser at save time.
        group.MapPost("/pessimistic", async (
            ConcurrencyDemoRequest request,
            ISqlConnectionFactory connectionFactory,
            ProductLockDialect dialect,
            CancellationToken ct) =>
        {
            async Task<int> LockThenDecrementAsync()
            {
                await using var connection = connectionFactory.Create();
                await connection.OpenAsync(ct);
                await using var transaction = await connection.BeginTransactionAsync(ct);

                var stockAtLock = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    dialect.LockAndReadStockSql, new { id = request.ProductId }, transaction, cancellationToken: ct));

                await connection.ExecuteAsync(new CommandDefinition(
                    dialect.DecrementStockSql,
                    new { id = request.ProductId, quantity = request.Quantity }, transaction, cancellationToken: ct));

                await transaction.CommitAsync(ct);
                return stockAtLock;
            }

            var stockReads = await Task.WhenAll(LockThenDecrementAsync(), LockThenDecrementAsync());
            return Results.Ok(new { stockReadsUnderLock = stockReads });
        });

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
