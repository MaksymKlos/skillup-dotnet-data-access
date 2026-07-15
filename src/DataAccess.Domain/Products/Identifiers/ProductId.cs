using DataAccess.Domain.Common;

namespace DataAccess.Domain.Products.Identifiers;

public readonly record struct ProductId(Guid Value) : IStronglyTypedId
{
    public static ProductId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
