using System.Text;
using DataAccess.Api.Messaging;
using DataAccess.Infrastructure.EfCore;
using DataAccess.Infrastructure.EfCore.Inbox;
using DataAccess.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Xunit;

namespace DataAccess.IntegrationTests;

public sealed class OutboxEndToEndTests(OutboxEndToEndFixture fixture) : IClassFixture<OutboxEndToEndFixture>
{
    [Fact]
    public async Task Placed_order_event_is_relayed_to_rabbitmq_and_consumed()
    {
        var ct = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var channel = await fixture.Connection.CreateChannelAsync(cancellationToken: ct);
        await OrderEventsTopology.DeclareAsync(channel, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            received.TrySetResult(Encoding.UTF8.GetString(ea.Body.Span));
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
        };
        await channel.BasicConsumeAsync(OrderEventsTopology.QueueName, autoAck: false, consumer, cancellationToken: ct);

        var product = await TestData.CreateProductAsync(fixture.Services, stock: 5, ct);
        var orderId = await TestData.PlaceOrderAsync(fixture.Services, [(product, 1)], ct);

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

    [Fact]
    public async Task Duplicate_delivery_is_processed_once()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var consumer = new OrderEventsConsumer(
            fixture.Connection,
            fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            NullLogger<OrderEventsConsumer>.Instance);
        await consumer.StartAsync(ct);

        var messageId = Guid.NewGuid().ToString();
        await using var channel = await fixture.Connection.CreateChannelAsync(cancellationToken: ct);
        await OrderEventsTopology.DeclareAsync(channel, ct);

        for (var i = 0; i < 2; i++)
        {
            var properties = new BasicProperties
            {
                MessageId = messageId,
                Type = "OrderPlaced",
                ContentType = "application/json",
            };
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: OrderEventsTopology.QueueName,
                mandatory: false,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes("""{"demo":true}"""),
                cancellationToken: ct);
        }

        try
        {
            InboxMessage? recorded = null;
            for (var attempt = 0; attempt < 40 && recorded is null; attempt++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
                await using var scope = fixture.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                recorded = await db.InboxMessages.AsNoTracking()
                    .SingleOrDefaultAsync(m => m.MessageId == messageId, ct);
            }

            recorded.ShouldNotBeNull("the consumer should record the message id");

            await using var verifyScope = fixture.Services.CreateAsyncScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var count = await verifyDb.InboxMessages.CountAsync(m => m.MessageId == messageId, ct);
            count.ShouldBe(1);
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }
}
