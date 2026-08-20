using DataAccess.Domain.Products;
using DataAccess.Domain.Products.Identifiers;

namespace DataAccess.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetAsync(ProductId id, CancellationToken ct = default);

    Task<IReadOnlyList<Product>> GetManyAsync(
        IReadOnlyCollection<ProductId> ids,
        CancellationToken ct = default);

    void Add(Product product);
}
