using DataAccess.Application.Common.Paging;
using DataAccess.Application.Orders.Queries;
using DataAccess.Domain.Orders;
using DataAccess.Infrastructure.Dapper.Common;
using global::Dapper;

namespace DataAccess.Infrastructure.Dapper.Orders;

public sealed class DapperOrderQueries(ISqlConnectionFactory connectionFactory, OrderDialect dialect)
    : IOrderQueries
{
    private const int MaxPageSize = 100;

    public async Task<OrderDetailsDto?> GetOrderDetailsAsync(Guid orderId, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.Create();

        OrderHeaderRow? header = null;
        var lines = new List<OrderLineRow>();

        var command = new CommandDefinition(dialect.OrderDetailsSql, new { orderId }, cancellationToken: ct);
        await connection.QueryAsync<OrderHeaderRow, OrderLineRow?, OrderHeaderRow>(
            command,
            (order, line) =>
            {
                header ??= order;
                if (line is not null)
                {
                    lines.Add(line);
                }

                return order;
            },
            splitOn: "LineId");

        if (header is null)
        {
            return null;
        }

        var lineDtos = lines
            .Select(l => new OrderLineDto(l.ProductId, l.Quantity, l.UnitPrice, l.UnitPrice * l.Quantity))
            .ToArray();

        return new OrderDetailsDto(
            header.Id,
            header.CustomerId,
            ((OrderStatus)header.Status).ToString(),
            lineDtos.Sum(l => l.LineTotal),
            lines.Count > 0 ? lines[0].Currency : string.Empty,
            header.CreatedAt,
            lineDtos);
    }

    public async Task<KeysetPage<OrderSummaryDto>> ListOrderSummariesAsync(
        KeysetPageRequest request,
        CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var hasCursor = request is { AfterCreatedAt: not null, AfterId: not null };

        object parameters = hasCursor
            ? new { limit = pageSize + 1, afterCreatedAt = request.AfterCreatedAt!.Value, afterId = request.AfterId!.Value }
            : new { limit = pageSize + 1 };

        await using var connection = connectionFactory.Create();
        var command = new CommandDefinition(dialect.BuildOrderSummariesSql(hasCursor), parameters, cancellationToken: ct);
        var rows = (await connection.QueryAsync<OrderSummaryRow>(command)).ToArray();

        var summaries = rows
            .Select(r => new OrderSummaryDto(
                r.OrderId, r.CustomerId, ((OrderStatus)r.Status).ToString(),
                r.TotalAmount, r.Currency, r.LineCount, r.CreatedAt))
            .ToArray();

        return KeysetPageBuilder.Build(summaries, pageSize, s => s.CreatedAt, s => s.OrderId);
    }

    private sealed record OrderHeaderRow(Guid Id, Guid CustomerId, int Status, DateTimeOffset CreatedAt);

    private sealed record OrderLineRow(Guid LineId, Guid ProductId, int Quantity, decimal UnitPrice, string Currency);

    private sealed record OrderSummaryRow(
        Guid OrderId,
        Guid CustomerId,
        int Status,
        DateTimeOffset CreatedAt,
        decimal TotalAmount,
        string Currency,
        int LineCount);
}
