using DataAccess.Domain.Common;
using DataAccess.Domain.Products;
using DataAccess.Domain.Products.Events;
using DataAccess.Domain.Products.Identifiers;
using Shouldly;
using Xunit;

namespace DataAccess.UnitTests.Domain;

public class ProductTests
{
    private static Product NewProduct(int stock = 10)
        => Product.Create(ProductId.New(), "SKU-1", new Money(10m, "USD"), stock);

    [Fact]
    public void Decrease_reduces_stock()
    {
        var product = NewProduct(10);

        product.Decrease(3);

        product.Stock.ShouldBe(7);
    }

    [Fact]
    public void Decrease_below_zero_throws()
    {
        var product = NewProduct(2);

        Should.Throw<DomainException>(() => product.Decrease(5));
    }

    [Fact]
    public void Decrease_with_non_positive_quantity_throws()
    {
        var product = NewProduct(10);

        Should.Throw<DomainException>(() => product.Decrease(0));
    }

    [Fact]
    public void Decrease_to_zero_raises_StockDepleted()
    {
        var product = NewProduct(3);

        product.Decrease(3);

        product.DomainEvents.OfType<StockDepleted>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Restock_increases_stock()
    {
        var product = NewProduct(1);

        product.Restock(4);

        product.Stock.ShouldBe(5);
    }

    [Fact]
    public void Restock_with_non_positive_quantity_throws()
    {
        var product = NewProduct(1);

        Should.Throw<DomainException>(() => product.Restock(0));
    }

    [Fact]
    public void Create_with_blank_sku_throws()
    {
        Should.Throw<DomainException>(() => Product.Create(ProductId.New(), " ", new Money(10m, "USD"), 1));
    }

    [Fact]
    public void Create_with_negative_stock_throws()
    {
        Should.Throw<DomainException>(() => Product.Create(ProductId.New(), "SKU-1", new Money(10m, "USD"), -1));
    }

    [Fact]
    public void Created_product_exposes_price_and_sku()
    {
        var product = Product.Create(ProductId.New(), "SKU-1", new Money(12.50m, "USD"), 5);

        product.Sku.ShouldBe("SKU-1");
        product.Price.ShouldBe(new Money(12.50m, "USD"));
    }
}
