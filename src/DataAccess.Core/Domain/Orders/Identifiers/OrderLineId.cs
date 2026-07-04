namespace DataAccess.Core.Domain.Orders.Identifiers;

public readonly record struct OrderLineId(Guid Value)
{
    public static OrderLineId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
