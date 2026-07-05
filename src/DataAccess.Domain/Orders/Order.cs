using DataAccess.Domain.Common;
using DataAccess.Domain.Orders.Events;
using DataAccess.Domain.Orders.Identifiers;
using DataAccess.Domain.Products.Identifiers;

namespace DataAccess.Domain.Orders;

/// <summary>
/// Order aggregate root. Owns its lines and guards all invariants: lines change only
/// while the order is a draft, an order cannot be placed without lines, and all lines
/// must share one currency.
/// </summary>
public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }

    private Order(OrderId id, CustomerId customerId, DateTimeOffset createdAt) : base(id)
    {
        CustomerId = customerId;
        Status = OrderStatus.Draft;
        CreatedAt = createdAt;
    }

    public CustomerId CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public Money Total => _lines.Count == 0
        ? Money.Zero("USD")
        : _lines.Select(line => line.LineTotal).Aggregate(static (a, b) => a.Add(b));

    public static Order CreateDraft(CustomerId customerId, DateTimeOffset createdAt)
        => new(OrderId.New(), customerId, createdAt);

    public void AddLine(ProductId productId, int quantity, Money unitPrice)
    {
        EnsureDraft();

        if (_lines.Count > 0 && _lines[0].UnitPrice.Currency != unitPrice.Currency)
        {
            throw new DomainException("All order lines must share the same currency.");
        }

        var existing = _lines.FirstOrDefault(line => line.ProductId == productId);
        if (existing is not null)
        {
            existing.ChangeQuantity(existing.Quantity + quantity);
        }
        else
        {
            _lines.Add(new OrderLine(OrderLineId.New(), productId, quantity, unitPrice));
        }
    }

    public void RemoveLine(ProductId productId)
    {
        EnsureDraft();
        _lines.RemoveAll(line => line.ProductId == productId);
    }

    public void Place()
    {
        EnsureDraft();

        if (_lines.Count == 0)
        {
            throw new DomainException("Cannot place an order without lines.");
        }

        Status = OrderStatus.Placed;
        RaiseDomainEvent(new OrderPlaced(Id, CustomerId, Total.Amount, Total.Currency, DateTimeOffset.UtcNow));
    }

    public void MarkPaid()
    {
        if (Status != OrderStatus.Placed)
        {
            throw new DomainException("Only a placed order can be marked as paid.");
        }

        Status = OrderStatus.Paid;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Paid)
        {
            throw new DomainException("A paid order cannot be cancelled.");
        }

        if (Status is OrderStatus.Cancelled)
        {
            throw new DomainException("Order is already cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainException("Order can only be modified while in Draft status.");
        }
    }
}
