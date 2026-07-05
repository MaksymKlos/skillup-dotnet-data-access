using DataAccess.Domain.Orders;
using DataAccess.Domain.Orders.Identifiers;

namespace DataAccess.Application.Orders;

/// <summary>
/// Command-side access to the <see cref="Order"/> aggregate: loads a whole
/// aggregate to mutate it, or registers a new one. Reads for display go through
/// the query side (IOrderQueries), not here.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetAsync(OrderId id, CancellationToken ct = default);

    void Add(Order order);
}
