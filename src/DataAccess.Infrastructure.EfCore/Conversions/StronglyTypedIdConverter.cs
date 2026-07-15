using System.Linq.Expressions;
using DataAccess.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DataAccess.Infrastructure.EfCore.Conversions;

// One converter for every strongly-typed id (id <-> Guid), discovered and registered in bulk
// by AppDbContext.ConfigureConventions.
public sealed class StronglyTypedIdConverter<TId>()
    : ValueConverter<TId, Guid>(id => id.Value, FromGuid())
    where TId : struct, IStronglyTypedId
{
    // Build `value => new TId(value)` from the record struct's single-Guid constructor.
    private static Expression<Func<Guid, TId>> FromGuid()
    {
        var ctor = typeof(TId).GetConstructor([typeof(Guid)])
            ?? throw new InvalidOperationException(
                $"{typeof(TId)} must have a constructor taking a single Guid.");

        var value = Expression.Parameter(typeof(Guid), "value");
        return Expression.Lambda<Func<Guid, TId>>(Expression.New(ctor, value), value);
    }
}
