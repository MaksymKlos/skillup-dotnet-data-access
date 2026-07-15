using DataAccess.Domain.Common;

namespace DataAccess.Domain.Orders.Identifiers;

public readonly record struct OrderLineId(Guid Value) : IStronglyTypedId
{
    public static OrderLineId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
