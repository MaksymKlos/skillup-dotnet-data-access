using System.Text;
using DataAccess.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace DataAccess.Api.Messaging;

public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IConnection connection,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await OrderEventsTopology.DeclareAsync(channel, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(channel, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox processing iteration failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(IChannel channel, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            var properties = new BasicProperties
            {
                MessageId = message.Id.ToString(),
                Type = message.Type,
                ContentType = "application/json",
                Persistent = true,
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: OrderEventsTopology.QueueName,
                mandatory: false,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(message.Content),
                cancellationToken: ct);

            message.ProcessedAt = timeProvider.GetUtcNow();
        }

        if (pending.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Published {Count} outbox message(s)", pending.Count);
        }
    }
}
