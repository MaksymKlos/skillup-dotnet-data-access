using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DataAccess.Api.Messaging;

public sealed class OrderEventsConsumer(
    IConnection connection,
    ILogger<OrderEventsConsumer> logger) : BackgroundService, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, byte> _seen = new();

    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        _channel = channel;

        await channel.QueueDeclareAsync(
            OrderEventsTopology.QueueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var messageId = ea.BasicProperties.MessageId ?? string.Empty;

            if (_seen.TryAdd(messageId, 0))
            {
                var type = ea.BasicProperties.Type ?? "unknown";
                var body = Encoding.UTF8.GetString(ea.Body.Span);
                logger.LogInformation("Received {Type} ({MessageId}): {Body}", type, messageId, body);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        };

        await channel.BasicConsumeAsync(
            OrderEventsTopology.QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
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
