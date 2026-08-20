using DataAccess.Domain.Common;
using DataAccess.Domain.Products.Events;
using DataAccess.Domain.Products.Identifiers;

namespace DataAccess.Domain.Products;

// The optimistic-concurrency token (xmin / rowversion) is a shadow property configured
// in the EF layer, so it does not appear here.
public sealed class Product : AggregateRoot<ProductId>
{
    private Product() { }

    private Product(ProductId id, string sku, Money price, int stock) : base(id)
    {
        Sku = sku;
        Price = price;
        Stock = stock;
    }

    public string Sku { get; private set; } = null!;

    public Money Price { get; private set; } = null!;

    public int Stock { get; private set; }

    public static Product Create(ProductId id, string sku, Money price, int initialStock)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU is required.");
        }

        if (initialStock < 0)
        {
            throw new DomainException("Initial stock cannot be negative.");
        }

        return new Product(id, sku, price, initialStock);
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
            RaiseDomainEvent(new StockDepleted(Id, Sku));
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
