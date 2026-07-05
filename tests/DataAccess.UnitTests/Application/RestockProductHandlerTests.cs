using DataAccess.Application.Abstractions;
using DataAccess.Application.Products;
using DataAccess.Application.Products.Commands;
using DataAccess.Domain.Common;
using DataAccess.Domain.Products;
using DataAccess.Domain.Products.Identifiers;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DataAccess.UnitTests.Application;

public class RestockProductHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RestockProductCommandHandler CreateHandler() => new(_products, _unitOfWork);

    [Fact]
    public async Task Restocks_existing_product()
    {
        var id = ProductId.New();
        var product = Product.Create(id, "SKU", new Money(10m, "USD"), 1);
        _products.GetAsync(id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await CreateHandler().HandleAsync(
            new RestockProductCommand(id.Value, 4),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        product.Stock.ShouldBe(5);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_product_is_not_found()
    {
        _products.GetAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await CreateHandler().HandleAsync(
            new RestockProductCommand(Guid.NewGuid(), 4),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
