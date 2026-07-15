using DataAccess.Application.Orders.Queries;
using DataAccess.Infrastructure.Dapper.Common;
using Shouldly;
using Xunit;

namespace DataAccess.UnitTests.Infrastructure;

public class KeysetPageBuilderTests
{
    private static OrderSummaryDto Summary(int minute)
        => new(Guid.NewGuid(), Guid.NewGuid(), "Placed", 10m, "USD", 1,
            DateTimeOffset.UnixEpoch.AddMinutes(minute));

    [Fact]
    public void Fewer_rows_than_page_size_reports_no_more()
    {
        var page = KeysetPageBuilder.Build([Summary(1), Summary(2)], 5, s => s.CreatedAt, s => s.OrderId);

        page.Items.Count.ShouldBe(2);
        page.HasMore.ShouldBeFalse();
        page.NextCreatedAt.ShouldBeNull();
        page.NextId.ShouldBeNull();
    }

    [Fact]
    public void Exactly_page_size_reports_no_more()
    {
        var page = KeysetPageBuilder.Build([Summary(1), Summary(2)], 2, s => s.CreatedAt, s => s.OrderId);

        page.Items.Count.ShouldBe(2);
        page.HasMore.ShouldBeFalse();
        page.NextCreatedAt.ShouldBeNull();
    }

    [Fact]
    public void Extra_row_reports_more_and_sets_cursor_to_last_kept_item()
    {
        var rows = new[] { Summary(1), Summary(2), Summary(3) };

        var page = KeysetPageBuilder.Build(rows, 2, s => s.CreatedAt, s => s.OrderId);

        page.Items.Count.ShouldBe(2);
        page.HasMore.ShouldBeTrue();
        page.NextId.ShouldBe(page.Items[^1].OrderId);
        page.NextCreatedAt.ShouldBe(page.Items[^1].CreatedAt);
        page.Items.ShouldNotContain(rows[2]); // the probe row is dropped
    }

    [Fact]
    public void Empty_input_returns_empty_page()
    {
        var page = KeysetPageBuilder.Build([], 5, (OrderSummaryDto s) => s.CreatedAt, s => s.OrderId);

        page.Items.ShouldBeEmpty();
        page.HasMore.ShouldBeFalse();
        page.NextCreatedAt.ShouldBeNull();
        page.NextId.ShouldBeNull();
    }
}
