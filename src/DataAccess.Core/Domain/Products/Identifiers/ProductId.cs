namespace DataAccess.Core.Domain.Products.Identifiers;

public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
