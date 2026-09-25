using MongoDB.Driver;

namespace MyMusic.Mongo.Db;

/// <summary>
/// One client session per request. Repositories issue every catalog command
/// through it, so commands made inside <see cref="MongoTransactionRunner"/>
/// join its transaction.
/// </summary>
public sealed class MongoSession(IMongoClient client) : IDisposable
{
    private IClientSessionHandle? _handle;

    public IClientSessionHandle Handle => _handle ??= client.StartSession();

    public void Dispose() => _handle?.Dispose();
}
