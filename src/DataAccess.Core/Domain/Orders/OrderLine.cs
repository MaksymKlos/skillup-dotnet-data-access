using DataAccess.Core.Domain.Common;
using DataAccess.Core.Domain.Orders.Identifiers;
using DataAccess.Core.Domain.Products.Identifiers;

namespace DataAccess.Core.Domain.Orders;

/// <summary>
/// A line inside an <see cref="Order"/> aggregate. Only the root creates or changes
/// lines, which is why the constructor and mutators are internal.
/// </summary>
public sealed class OrderLine : Entity<OrderLineId>
{
    private OrderLine() { }

    internal OrderLine(OrderLineId id, ProductId productId, int quantity, Money unitPrice)
        : base(id)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Order line quantity must be positive.");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public ProductId ProductId { get; private set; }

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; } = default!;

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Order line quantity must be positive.");
        }

        Quantity = quantity;
    }
}
