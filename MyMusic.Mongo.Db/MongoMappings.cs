using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MyMusic.Core.Models;

namespace MyMusic.Mongo.Db;

/// <summary>
/// BSON mappings for the Core models, kept here so Core has no MongoDB
/// dependency. Ids are strings in C# and ObjectIds in the database.
/// </summary>
public static class MongoMappings
{
    private static int _registered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;
        }

        ConventionRegistry.Register(
            "MyMusic",
            new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) },
            type => type.Namespace == typeof(Artist).Namespace);

        MapWithObjectId<Artist>(a => a.Id);
        MapWithObjectId<Music>(m => m.Id, map =>
            // References are ObjectIds too, like the _id they point at.
            map.MapMember(m => m.ArtistId).SetSerializer(new StringSerializer(BsonType.ObjectId)));
        MapWithObjectId<Composer>(c => c.Id);
        MapWithObjectId<User>(u => u.Id);
    }

    private static void MapWithObjectId<T>(
        System.Linq.Expressions.Expression<Func<T, string>> id,
        Action<BsonClassMap<T>>? configure = null)
    {
        BsonClassMap.RegisterClassMap<T>(map =>
        {
            map.AutoMap();
            map.MapIdMember(id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            configure?.Invoke(map);
        });
    }
}
