using DataAccess.Application.Orders;
using DataAccess.Domain.Orders;
using DataAccess.Domain.Orders.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Infrastructure.EfCore.Repositories;

public sealed class EfOrderRepository(AppDbContext context) : IOrderRepository
{
    public async Task<Order?> GetAsync(OrderId id, CancellationToken ct = default)
        => await context.Orders.FirstOrDefaultAsync(order => order.Id == id, ct);

    public void Add(Order order) => context.Orders.Add(order);
}
