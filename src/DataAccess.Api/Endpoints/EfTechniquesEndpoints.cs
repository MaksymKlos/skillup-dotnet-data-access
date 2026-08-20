using DataAccess.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Api.Endpoints;

// A small tour of EF Core query features beyond load-mutate-save. These run set-based SQL or raw SQL
// directly; note that ExecuteUpdate/ExecuteDelete bypass the change tracker and SaveChanges, so they
// do NOT go through the Outbox interceptor — they are for maintenance, not for domain mutations.
public static class EfTechniquesEndpoints
{
    public static IEndpointRouteBuilder MapEfTechniquesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ef").WithTags("EF Techniques");

        // Bulk update: one UPDATE statement, no entities materialized or tracked.
        group.MapPost("/products/bulk-restock", async (
            AppDbContext dbContext,
            CancellationToken ct,
            int belowStock = 5,
            int addStock = 10) =>
        {
            var updated = await dbContext.Products
                .Where(p => p.Stock < belowStock)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + addStock), ct);

            return Results.Ok(new { updated });
        });

        // Bulk delete: one DELETE statement for maintenance (purge already-relayed outbox rows).
        group.MapDelete("/outbox/processed", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            var deleted = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt != null)
                .ExecuteDeleteAsync(ct);

            return Results.Ok(new { deleted });
        });

        // Raw SQL with LINQ composed on top: the FromSql query is the source, the Where/OrderBy are
        // translated and appended by EF, so the raw part stays provider-neutral (lowercase table name).
        group.MapGet("/outbox/raw", async (
            AppDbContext dbContext,
            CancellationToken ct,
            string type = "OrderPlaced") =>
        {
            var rows = await dbContext.OutboxMessages
                .FromSql($"SELECT * FROM outbox_messages")
                .Where(m => m.Type == type)
                .OrderByDescending(m => m.OccurredAt)
                .Take(50)
                .AsNoTracking()
                .Select(m => new { m.Id, m.Type, m.OccurredAt, m.ProcessedAt })
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        // Split query: load orders and their owned lines as separate SQL statements instead of one
        // join, avoiding the cartesian row explosion when an aggregate has many child rows.
        group.MapGet("/orders/split", async (
            AppDbContext dbContext,
            CancellationToken ct,
            int take = 20) =>
        {
            var orders = await dbContext.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .OrderBy(o => o.CreatedAt)
                .Take(take)
                .ToListAsync(ct);

            var summary = orders.Select(o => new { Id = o.Id.Value, Lines = o.Lines.Count });
            return Results.Ok(summary);
        });

        return app;
    }
}
