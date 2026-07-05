using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Domain.Common;
using DataAccess.Domain.Products;
using DataAccess.Domain.Products.Identifiers;

namespace DataAccess.Application.Products.Commands;

public sealed record CreateProductCommand(string Sku, decimal Price, string Currency, int InitialStock);

public sealed class CreateProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        var product = Product.Create(
            ProductId.New(),
            command.Sku,
            new Money(command.Price, command.Currency),
            command.InitialStock);

        products.Add(product);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(product.Id.Value);
    }
}
