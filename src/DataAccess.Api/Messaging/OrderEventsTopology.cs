using RabbitMQ.Client;

namespace DataAccess.Api.Messaging;

public static class OrderEventsTopology
{
    public const string QueueName = "order-events";

    public static Task DeclareAsync(IChannel channel, CancellationToken ct = default)
        => channel.QueueDeclareAsync(
            QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
}
