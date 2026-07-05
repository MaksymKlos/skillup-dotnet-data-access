namespace DataAccess.Domain.Common;

/// <summary>
/// Base class for entities: objects with an identity that persists over time.
/// Two entities are equal when they are the same type and share the same Id,
/// regardless of their other field values.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    protected Entity() { }

    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType()
               && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
