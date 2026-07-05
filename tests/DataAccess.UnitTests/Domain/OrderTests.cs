using DataAccess.Domain.Common;
using DataAccess.Domain.Orders;
using DataAccess.Domain.Orders.Events;
using DataAccess.Domain.Orders.Identifiers;
using DataAccess.Domain.Products.Identifiers;
using Shouldly;
using Xunit;

namespace DataAccess.UnitTests.Domain;

public class OrderTests
{
    private static Order NewDraft() => Order.CreateDraft(CustomerId.New(), DateTimeOffset.UtcNow);

    [Fact]
    public void Placing_an_empty_order_throws()
    {
        var order = NewDraft();

        Should.Throw<DomainException>(order.Place);
    }

    [Fact]
    public void Adding_a_line_increases_the_total()
    {
        var order = NewDraft();

        order.AddLine(ProductId.New(), 2, new Money(10m, "USD"));

        order.Total.Amount.ShouldBe(20m);
    }

    [Fact]
    public void Adding_the_same_product_twice_merges_quantity()
    {
        var order = NewDraft();
        var product = ProductId.New();

        order.AddLine(product, 2, new Money(10m, "USD"));
        order.AddLine(product, 3, new Money(10m, "USD"));

        order.Lines.ShouldHaveSingleItem().Quantity.ShouldBe(5);
    }

    [Fact]
    public void Removing_a_line_updates_the_total()
    {
        var order = NewDraft();
        var product = ProductId.New();
        order.AddLine(product, 2, new Money(10m, "USD"));

        order.RemoveLine(product);

        order.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void Cannot_modify_order_after_it_is_placed()
    {
        var order = NewDraft();
        order.AddLine(ProductId.New(), 1, new Money(10m, "USD"));
        order.Place();

        Should.Throw<DomainException>(() => order.AddLine(ProductId.New(), 1, new Money(5m, "USD")));
    }

    [Fact]
    public void Placing_an_order_raises_OrderPlaced()
    {
        var order = NewDraft();
        order.AddLine(ProductId.New(), 1, new Money(10m, "USD"));

        order.Place();

        order.DomainEvents.OfType<OrderPlaced>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Adding_a_line_with_non_positive_quantity_throws()
    {
        var order = NewDraft();

        Should.Throw<DomainException>(() => order.AddLine(ProductId.New(), 0, new Money(10m, "USD")));
    }

    [Fact]
    public void Mixing_currencies_across_lines_throws()
    {
        var order = NewDraft();
        order.AddLine(ProductId.New(), 1, new Money(10m, "USD"));

        Should.Throw<DomainException>(() => order.AddLine(ProductId.New(), 1, new Money(10m, "EUR")));
    }

    [Fact]
    public void Marking_a_placed_order_as_paid_sets_status_paid()
    {
        var order = NewDraft();
        order.AddLine(ProductId.New(), 1, new Money(10m, "USD"));
        order.Place();

        order.MarkPaid();

        order.Status.ShouldBe(OrderStatus.Paid);
    }

    [Fact]
    public void Marking_a_draft_order_as_paid_throws()
    {
        var order = NewDraft();

        Should.Throw<DomainException>(order.MarkPaid);
    }

    [Fact]
    public void Cancelling_a_draft_order_sets_status_cancelled()
    {
        var order = NewDraft();

        order.Cancel();

        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancelling_a_paid_order_throws()
    {
        var order = NewDraft();
        order.AddLine(ProductId.New(), 1, new Money(10m, "USD"));
        order.Place();
        order.MarkPaid();

        Should.Throw<DomainException>(order.Cancel);
    }
}
