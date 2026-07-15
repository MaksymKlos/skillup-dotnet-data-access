using DataAccess.Domain.Common;

namespace DataAccess.Domain.Orders.Identifiers;

public readonly record struct OrderId(Guid Value) : IStronglyTypedId
{
    public static OrderId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
