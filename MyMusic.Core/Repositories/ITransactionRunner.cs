namespace MyMusic.Core.Repositories;

/// <summary>
/// Runs a unit of work atomically: every repository call made inside it
/// commits together or not at all. The work may be retried on a transient
/// conflict, so it must not have side effects outside the repositories.
/// </summary>
public interface ITransactionRunner
{
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default);
}
