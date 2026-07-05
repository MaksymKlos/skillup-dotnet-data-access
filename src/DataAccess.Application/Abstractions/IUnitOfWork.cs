namespace DataAccess.Application.Abstractions;

/// <summary>
/// Commits all changes tracked during one command as a single transaction.
/// Implemented by the EF Core DbContext in the infrastructure layer, so the
/// Domain and Application layers stay free of any EF dependency.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
