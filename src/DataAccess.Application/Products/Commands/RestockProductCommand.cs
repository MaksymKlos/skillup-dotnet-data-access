using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Domain.Products.Identifiers;

namespace DataAccess.Application.Products.Commands;

public sealed record RestockProductCommand(Guid ProductId, int Quantity);

public sealed class RestockProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : ICommandHandler<RestockProductCommand, Result>
{
    public async Task<Result> HandleAsync(RestockProductCommand command, CancellationToken ct = default)
    {
        var product = await products.GetAsync(new ProductId(command.ProductId), ct);
        if (product is null)
        {
            return Result.Failure($"Product {command.ProductId} was not found.");
        }

        product.Restock(command.Quantity);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
