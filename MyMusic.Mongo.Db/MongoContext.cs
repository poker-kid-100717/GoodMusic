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
