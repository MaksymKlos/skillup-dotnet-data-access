using DataAccess.Application.Abstractions;
using DataAccess.Application.Products;
using DataAccess.Application.Products.Commands;
using DataAccess.Domain.Products;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DataAccess.UnitTests.Application;

public class CreateProductHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateProductCommandHandler CreateHandler() => new(_products, _unitOfWork);

    [Fact]
    public async Task Creates_and_saves_product()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateProductCommand("SKU-1", 12.50m, "USD", 100),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        _products.Received(1).Add(Arg.Any<Product>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
