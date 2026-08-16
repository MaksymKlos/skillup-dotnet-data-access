using DataAccess.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Api.Endpoints;

public static class OutboxEndpoints
{
    public static IEndpointRouteBuilder MapOutboxEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/outbox", async (AppDbContext dbContext, CancellationToken ct, string status = "pending") =>
        {
            var query = dbContext.OutboxMessages.AsNoTracking();
            if (status.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.ProcessedAt == null);
            }

            var messages = await query
                .OrderBy(m => m.OccurredAt)
                .Take(100)
                .Select(m => new OutboxMessageDto(m.Id, m.Type, m.OccurredAt, m.ProcessedAt))
                .ToListAsync(ct);

            return Results.Ok(messages);
        }).WithTags("Outbox");

        return app;
    }

    public sealed record OutboxMessageDto(Guid Id, string Type, DateTimeOffset OccurredAt, DateTimeOffset? ProcessedAt);
}
