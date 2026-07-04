namespace DataAccess.Core.Domain.Orders.Identifiers;

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
