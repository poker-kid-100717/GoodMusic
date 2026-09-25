using MongoDB.Driver;
using MyMusic.Core.Repositories;

namespace MyMusic.Mongo.Db;

/// <summary>
/// Snapshot-isolated MongoDB transactions. Two transactions that write the
/// same document conflict, and the driver retries the loser from the start,
/// so concurrent song and artist writes behave as if they ran one at a time.
/// Needs a replica set (every Atlas cluster is one).
/// </summary>
public class MongoTransactionRunner(MongoSession session) : ITransactionRunner
{
    private static readonly TransactionOptions Options = new(ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority);

    public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default) =>
        session.Handle.WithTransactionAsync((_, token) => work(token), Options, cancellationToken);
}
