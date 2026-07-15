using DataAccess.Domain.Orders;
using DataAccess.Domain.Products;
using DataAccess.Infrastructure.EfCore;
using DataAccess.Infrastructure.EfCore.Outbox;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace DataAccess.UnitTests.Infrastructure;

/// <summary>
/// Validates that both provider models are internally consistent — every configuration,
/// converter, owned type and concurrency token resolves — by building the EF Core model
/// without opening a database connection. Real persistence is exercised in the integration
/// tests (Step 8).
/// </summary>
public class ModelBuildsTests
{
    private static PostgresAppDbContext NewPostgresContext()
    {
        var options = new DbContextOptionsBuilder<PostgresAppDbContext>()
            .UseNpgsql("Host=localhost;Database=ordersdb;Username=u;Password=p")
            .Options;
        return new PostgresAppDbContext(options);
    }

    private static SqlServerAppDbContext NewSqlServerContext()
    {
        var options = new DbContextOptionsBuilder<SqlServerAppDbContext>()
            .UseSqlServer("Server=localhost;Database=OrdersDb;User Id=u;Password=p;")
            .Options;
        return new SqlServerAppDbContext(options);
    }

    [Fact]
    public void Postgres_model_builds()
    {
        using var context = NewPostgresContext();

        var model = context.Model;

        model.FindEntityType(typeof(Order)).ShouldNotBeNull();
        model.FindEntityType(typeof(Product)).ShouldNotBeNull();
        model.FindEntityType(typeof(OutboxMessage)).ShouldNotBeNull();
    }

    [Fact]
    public void SqlServer_model_builds()
    {
        using var context = NewSqlServerContext();

        var model = context.Model;

        model.FindEntityType(typeof(Order)).ShouldNotBeNull();
        model.FindEntityType(typeof(Product)).ShouldNotBeNull();
        model.FindEntityType(typeof(OutboxMessage)).ShouldNotBeNull();
    }

    [Fact]
    public void Order_lines_are_an_owned_collection_with_owned_unit_price()
    {
        using var context = NewPostgresContext();

        var lines = context.Model.FindEntityType(typeof(Order))!.FindNavigation(nameof(Order.Lines));
        lines.ShouldNotBeNull();
        lines.ForeignKey.IsOwnership.ShouldBeTrue();

        var lineType = lines.TargetEntityType;
        lineType.FindNavigation(nameof(OrderLine.UnitPrice))!.ForeignKey.IsOwnership.ShouldBeTrue();
    }

    [Fact]
    public void Order_status_is_stored_as_int()
    {
        using var context = NewPostgresContext();

        var status = context.Model.FindEntityType(typeof(Order))!.FindProperty(nameof(Order.Status));
        status.ShouldNotBeNull();
        status.GetProviderClrType().ShouldBe(typeof(int));
    }

    [Fact]
    public void Strongly_typed_ids_are_stored_as_guid()
    {
        using var context = NewPostgresContext();

        var orderId = context.Model.FindEntityType(typeof(Order))!.FindProperty(nameof(Order.Id));
        orderId.ShouldNotBeNull();
        orderId.GetValueConverter()!.ProviderClrType.ShouldBe(typeof(Guid));

        var productId = context.Model.FindEntityType(typeof(Product))!.FindProperty(nameof(Product.Id));
        productId.ShouldNotBeNull();
        productId.GetValueConverter()!.ProviderClrType.ShouldBe(typeof(Guid));
    }

    [Fact]
    public void Required_string_columns_are_not_nullable()
    {
        using var context = NewPostgresContext();
        var model = context.Model;

        model.FindEntityType(typeof(Product))!.FindProperty(nameof(Product.Sku))!
            .IsNullable.ShouldBeFalse();

        var outbox = model.FindEntityType(typeof(OutboxMessage))!;
        outbox.FindProperty(nameof(OutboxMessage.Type))!.IsNullable.ShouldBeFalse();
        outbox.FindProperty(nameof(OutboxMessage.Content))!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Product_price_is_owned()
    {
        using var context = NewPostgresContext();

        var price = context.Model.FindEntityType(typeof(Product))!.FindNavigation(nameof(Product.Price));
        price.ShouldNotBeNull();
        price.ForeignKey.IsOwnership.ShouldBeTrue();
    }

    [Fact]
    public void Postgres_product_has_xmin_concurrency_token()
    {
        using var context = NewPostgresContext();

        var token = context.Model.FindEntityType(typeof(Product))!.FindProperty("xmin");
        token.ShouldNotBeNull();
        token.IsConcurrencyToken.ShouldBeTrue();
    }

    [Fact]
    public void SqlServer_product_has_rowversion_concurrency_token()
    {
        using var context = NewSqlServerContext();

        var token = context.Model.FindEntityType(typeof(Product))!.FindProperty("RowVersion");
        token.ShouldNotBeNull();
        token.IsConcurrencyToken.ShouldBeTrue();
    }
}
