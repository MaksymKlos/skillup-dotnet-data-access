using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Application.Orders.Commands;
using DataAccess.Application.Products.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.IntegrationTests.Infrastructure;

// Seeds data through the real command handlers, so tests exercise the write side end to end.
internal static class TestData
{
    public static async Task<Guid> CreateProductAsync(
        IServiceProvider services, int stock, CancellationToken ct, decimal price = 9.99m)
    {
        await using var scope = services.CreateAsyncScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateProductCommand, Result<Guid>>>();
        var result = await handler.HandleAsync(
            new CreateProductCommand($"SKU-{Guid.NewGuid():N}", price, "USD", stock), ct);
        return result.Value;
    }

    public static async Task<Guid> PlaceOrderAsync(
        IServiceProvider services, IReadOnlyCollection<(Guid ProductId, int Quantity)> items, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<PlaceOrderCommand, Result<Guid>>>();
        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            items.Select(item => new PlaceOrderItem(item.ProductId, item.Quantity)).ToArray());
        var result = await handler.HandleAsync(command, ct);
        return result.Value;
    }
}
