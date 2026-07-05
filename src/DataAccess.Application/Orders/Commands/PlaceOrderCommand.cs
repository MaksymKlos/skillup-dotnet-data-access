using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Application.Products;
using DataAccess.Domain.Orders;
using DataAccess.Domain.Orders.Identifiers;
using DataAccess.Domain.Products;
using DataAccess.Domain.Products.Identifiers;

namespace DataAccess.Application.Orders.Commands;

public sealed record PlaceOrderCommand(Guid CustomerId, IReadOnlyCollection<PlaceOrderItem> Items);

public sealed record PlaceOrderItem(Guid ProductId, int Quantity);

public sealed class PlaceOrderCommandHandler(
    IProductRepository products,
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<PlaceOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(PlaceOrderCommand command, CancellationToken ct = default)
    {
        if (command.Items.Count == 0)
        {
            return Result.Failure<Guid>("An order must contain at least one item.");
        }

        var productsById = await LoadProductsAsync(command, ct);

        var validation = ValidateItems(command, productsById);
        if (validation.IsFailure)
        {
            return Result.Failure<Guid>(validation.Error!);
        }

        var order = AssembleOrder(command, validation.Value);

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(order.Id.Value);
    }

    private async Task<Dictionary<ProductId, Product>> LoadProductsAsync(PlaceOrderCommand command, CancellationToken ct)
    {
        var ids = command.Items.Select(item => new ProductId(item.ProductId)).ToArray();
        var loaded = await products.GetManyAsync(ids, ct);
        return loaded.ToDictionary(product => product.Id);
    }

    // Validate everything before mutating anything, so a rejected order leaves no side effects.
    private static Result<IReadOnlyList<OrderItem>> ValidateItems(
        PlaceOrderCommand command,
        IReadOnlyDictionary<ProductId, Product> byId)
    {
        var items = new List<OrderItem>(command.Items.Count);

        foreach (var item in command.Items)
        {
            if (!byId.TryGetValue(new ProductId(item.ProductId), out var product))
            {
                return Result.Failure<IReadOnlyList<OrderItem>>($"Product {item.ProductId} was not found.");
            }

            if (product.Stock < item.Quantity)
            {
                return Result.Failure<IReadOnlyList<OrderItem>>($"Not enough stock for product {item.ProductId}.");
            }

            items.Add(new OrderItem(product, item.Quantity));
        }

        return Result.Success<IReadOnlyList<OrderItem>>(items);
    }

    private Order AssembleOrder(PlaceOrderCommand command, IReadOnlyList<OrderItem> items)
    {
        var order = Order.CreateDraft(new CustomerId(command.CustomerId), timeProvider.GetUtcNow());

        foreach (var (product, quantity) in items)
        {
            product.Decrease(quantity);
            order.AddLine(product.Id, quantity, product.Price);
        }

        order.Place();
        return order;
    }

    private readonly record struct OrderItem(Product Product, int Quantity);
}
