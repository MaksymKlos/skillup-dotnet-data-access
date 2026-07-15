namespace DataAccess.Domain.Common;

/// <summary>
/// Marker for strongly-typed identifiers (<c>readonly record struct XxxId(Guid Value)</c>).
/// The EF layer discovers every implementor and maps it to a plain <see cref="Guid"/> column
/// through a single generic converter, so a new id needs no per-type mapping code.
/// </summary>
public interface IStronglyTypedId
{
    Guid Value { get; }
}
