using DataAccess.Application.Abstractions;
using DataAccess.Application.Orders;
using DataAccess.Application.Orders.Commands;
using DataAccess.Application.Products;
using DataAccess.Domain.Common;
using DataAccess.Domain.Orders;
using DataAccess.Domain.Products;
using DataAccess.Domain.Products.Identifiers;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DataAccess.UnitTests.Application;

public class PlaceOrderHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private PlaceOrderCommandHandler CreateHandler() => new(_products, _orders, _unitOfWork, TimeProvider.System);

    private static Product ProductWith(ProductId id, int stock)
        => Product.Create(id, "SKU", new Money(10m, "USD"), stock);

    [Fact]
    public async Task Places_order_and_decreases_stock()
    {
        var productId = ProductId.New();
        var product = ProductWith(productId, 5);
        _products.GetManyAsync(Arg.Any<IReadOnlyCollection<ProductId>>(), Arg.Any<CancellationToken>())
            .Returns([product]);

        var result = await CreateHandler().HandleAsync(
            new PlaceOrderCommand(Guid.NewGuid(), [new PlaceOrderItem(productId.Value, 2)]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        product.Stock.ShouldBe(3);
        _orders.Received(1).Add(Arg.Any<Order>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_stock_is_insufficient()
    {
        var productId = ProductId.New();
        var product = ProductWith(productId, 1);
        _products.GetManyAsync(Arg.Any<IReadOnlyCollection<ProductId>>(), Arg.Any<CancellationToken>())
            .Returns([product]);

        var result = await CreateHandler().HandleAsync(
            new PlaceOrderCommand(Guid.NewGuid(), [new PlaceOrderItem(productId.Value, 5)]),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        product.Stock.ShouldBe(1);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_product_is_not_found()
    {
        _products.GetManyAsync(Arg.Any<IReadOnlyCollection<ProductId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateHandler().HandleAsync(
            new PlaceOrderCommand(Guid.NewGuid(), [new PlaceOrderItem(Guid.NewGuid(), 1)]),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_no_items()
    {
        var result = await CreateHandler().HandleAsync(
            new PlaceOrderCommand(Guid.NewGuid(), []),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
