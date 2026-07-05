namespace DataAccess.Application.Abstractions;

/// <summary>
/// Handles one command and returns a result. Invoked directly (no mediator).
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}
