using DataAccess.Application.Common.Paging;

namespace DataAccess.Application.Orders.Queries;

/// <summary>
/// Query-side (read) contract for orders. Implemented with Dapper. Returns flat
/// DTOs for display and never loads domain aggregates.
/// </summary>
public interface IOrderQueries
{
    Task<OrderDetailsDto?> GetOrderDetailsAsync(Guid orderId, CancellationToken ct = default);

    Task<KeysetPage<OrderSummaryDto>> ListOrderSummariesAsync(
        KeysetPageRequest request,
        CancellationToken ct = default);
}
