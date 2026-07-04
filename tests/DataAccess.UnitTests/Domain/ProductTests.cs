using DataAccess.Core.Domain.Common;
using DataAccess.Core.Domain.Products;
using DataAccess.Core.Domain.Products.Events;
using DataAccess.Core.Domain.Products.Identifiers;
using Shouldly;
using Xunit;

namespace DataAccess.UnitTests.Domain;

public class ProductTests
{
    private static Product NewProduct(int stock = 10)
        => Product.Create(ProductId.New(), "SKU-1", stock);

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
    public void Create_with_blank_sku_throws()
    {
        Should.Throw<DomainException>(() => Product.Create(ProductId.New(), " ", 1));
    }
}
