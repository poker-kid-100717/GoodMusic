using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MyMusic.Core.Models;

namespace MyMusic.Mongo.Db;

public class MongoContext
{
    public MongoContext(IMongoClient client, IOptions<MongoSettings> settings)
    {
        Database = client.GetDatabase(settings.Value.Database);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<Artist> Artists => Database.GetCollection<Artist>("artists");
    public IMongoCollection<Music> Musics => Database.GetCollection<Music>("musics");
    public IMongoCollection<Composer> Composers => Database.GetCollection<Composer>("composers");
    public IMongoCollection<User> Users => Database.GetCollection<User>("users");

    /// <summary>Prepares the database: legacy data, then indexes. Safe to run repeatedly.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await MigrateLegacyComposersAsync(cancellationToken);
        await EnsureIndexesAsync(cancellationToken);
    }

    /// <summary>
    /// Earlier versions stored composers in "Composers" with PascalCase
    /// fields. Merge them into "composers" with this version's field names,
    /// then drop the old collection. Does nothing once migrated.
    /// </summary>
    public async Task MigrateLegacyComposersAsync(CancellationToken cancellationToken = default)
    {
        const string legacyName = "Composers";

        var exists = await (await Database.ListCollectionNamesAsync(
                new ListCollectionNamesOptions { Filter = new BsonDocument("name", legacyName) }, cancellationToken))
            .AnyAsync(cancellationToken);
        if (!exists)
        {
            return;
        }

        var pipeline = new[]
        {
            new BsonDocument("$project", new BsonDocument
            {
                { "firstName", new BsonDocument("$ifNull", new BsonArray { "$firstName", "$FirstName", "" }) },
                { "lastName", new BsonDocument("$ifNull", new BsonArray { "$lastName", "$LastName", "" }) }
            }),
            new BsonDocument("$merge", new BsonDocument
            {
                { "into", "composers" },
                { "whenMatched", "keepExisting" },
                { "whenNotMatched", "insert" }
            })
        };

        await Database.GetCollection<BsonDocument>(legacyName)
            .AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken);
        await Database.DropCollectionAsync(legacyName, cancellationToken);
    }

    /// <summary>Creates the indexes the queries rely on. Safe to run repeatedly.</summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        await Musics.Indexes.CreateOneAsync(
            new CreateIndexModel<Music>(Builders<Music>.IndexKeys.Ascending(m => m.ArtistId)),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Ids arrive from URLs; anything that isn't a valid ObjectId can't
    /// match a document, so callers treat it as not found.
    /// </summary>
    public static bool IsValidId(string id) => ObjectId.TryParse(id, out _);
}
