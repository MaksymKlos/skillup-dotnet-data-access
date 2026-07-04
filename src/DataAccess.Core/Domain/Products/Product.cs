using DataAccess.Core.Domain.Common;
using DataAccess.Core.Domain.Products.Events;
using DataAccess.Core.Domain.Products.Identifiers;

namespace DataAccess.Core.Domain.Products;

/// <summary>
/// Product aggregate root — an item held in inventory, keyed by <see cref="ProductId"/>.
/// Guards that stock never goes negative and raises <see cref="StockDepleted"/> when it
/// hits zero. The optimistic-concurrency token (xmin / rowversion) is configured in the
/// EF layer as a shadow property, so it does not appear on the domain model.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    private Product() { }

    private Product(ProductId id, string sku, int stock) : base(id)
    {
        Sku = sku;
        Stock = stock;
    }

    public string Sku { get; private set; } = default!;

    public int Stock { get; private set; }

    public static Product Create(ProductId id, string sku, int initialStock)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU is required.");
        }

        if (initialStock < 0)
        {
            throw new DomainException("Initial stock cannot be negative.");
        }

        return new Product(id, sku, initialStock);
    }

    public void Decrease(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Decrease quantity must be positive.");
        }

        if (quantity > Stock)
        {
            throw new DomainException("Cannot decrease stock below zero.");
        }

        Stock -= quantity;

        if (Stock == 0)
        {
            RaiseDomainEvent(new StockDepleted(Id, Sku, DateTimeOffset.UtcNow));
        }
    }

    public void Restock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Restock quantity must be positive.");
        }

        Stock += quantity;
    }
}
