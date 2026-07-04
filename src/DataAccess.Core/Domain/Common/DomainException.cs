namespace DataAccess.Core.Domain.Common;

/// <summary>
/// Thrown when a domain invariant is violated (a situation correct calling code
/// should never produce). For *expected* business outcomes use Result at the
/// application layer instead of throwing.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
