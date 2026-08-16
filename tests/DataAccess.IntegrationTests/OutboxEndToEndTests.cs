using System.Text;
using DataAccess.Api.Messaging;
using DataAccess.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Xunit;

namespace DataAccess.IntegrationTests;

public sealed class OutboxEndToEndTests(OutboxEndToEndFixture fixture) : IClassFixture<OutboxEndToEndFixture>
{
    // Mirrors OrderEventsTopology.QueueName (internal to the Api).
    private const string QueueName = "order-events";

    [Fact]
    public async Task Placed_order_event_is_relayed_to_rabbitmq_and_consumed()
    {
        var ct = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var channel = await fixture.Connection.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            received.TrySetResult(Encoding.UTF8.GetString(ea.Body.Span));
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
        };
        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken: ct);

        var product = await TestData.CreateProductAsync(fixture.Services, stock: 5, ct);
        var orderId = await TestData.PlaceOrderAsync(fixture.Services, [(product, 1)], ct);

        // Run the real relay; it should pick up the captured event and publish it.
        var processor = new OutboxProcessor(
            fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            fixture.Connection,
            TimeProvider.System,
            NullLogger<OutboxProcessor>.Instance);
        await processor.StartAsync(ct);

        try
        {
            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(20), ct));
            completed.ShouldBe(received.Task, "the relay should publish the order event within the timeout");
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        (await received.Task).ShouldContain(orderId.ToString());
    }
}
