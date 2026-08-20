using System.Text;
using DataAccess.Infrastructure.EfCore;
using DataAccess.Infrastructure.EfCore.Inbox;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DataAccess.Api.Messaging;

// Idempotent consumer (the Inbox half of the Outbox/Inbox pattern). Transport delivery is
// at-least-once, so each message id is recorded in inbox_messages before it is acted on; a
// redelivered id is recognised and skipped, giving an effectively exactly-once outcome.
public sealed class OrderEventsConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OrderEventsConsumer> logger) : BackgroundService, IAsyncDisposable
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        _channel = channel;

        await OrderEventsTopology.DeclareAsync(channel, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => HandleAsync(channel, ea, stoppingToken);

        await channel.BasicConsumeAsync(
            OrderEventsTopology.QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var messageId = ea.BasicProperties.MessageId;

        if (!string.IsNullOrEmpty(messageId) && await TryRecordAsync(messageId, ct))
        {
            var type = ea.BasicProperties.Type ?? "unknown";
            var body = Encoding.UTF8.GetString(ea.Body.Span);
            logger.LogInformation("Received {Type} ({MessageId}): {Body}", type, messageId, body);
        }

        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
    }

    // Returns true if this id is new (and now recorded), false if it was already processed.
    private async Task<bool> TryRecordAsync(string messageId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await dbContext.InboxMessages.AnyAsync(m => m.MessageId == messageId, ct))
        {
            return false;
        }

        dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ProcessedAt = timeProvider.GetUtcNow(),
        });

        try
        {
            await dbContext.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // A concurrent delivery inserted the same id first; the PK conflict means it is a duplicate.
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        base.Dispose();
    }
}
