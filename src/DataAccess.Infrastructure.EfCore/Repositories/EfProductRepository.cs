using DataAccess.Application.Products;
using DataAccess.Domain.Products;
using DataAccess.Domain.Products.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Infrastructure.EfCore.Repositories;

public sealed class EfProductRepository(AppDbContext context) : IProductRepository
{
    public async Task<Product?> GetAsync(ProductId id, CancellationToken ct = default)
        => await context.Products.FirstOrDefaultAsync(product => product.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetManyAsync(
        IReadOnlyCollection<ProductId> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await context.Products
            .Where(product => ids.Contains(product.Id))
            .ToListAsync(ct);
    }

    public void Add(Product product) => context.Products.Add(product);
}
