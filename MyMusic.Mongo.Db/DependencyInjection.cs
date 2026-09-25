using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MyMusic.Core.Repositories;
using MyMusic.Mongo.Db.Repositories;

namespace MyMusic.Mongo.Db;

public static class DependencyInjection
{
    public static IServiceCollection AddMongoPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        MongoMappings.Register();

        services.Configure<MongoSettings>(configuration.GetSection(MongoSettings.SectionName));

        // MongoClient is thread-safe and pools connections: one per app.
        services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<MongoSettings>>().Value;
            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "MongoDB:ConnectionString is not configured. Set it via the MongoDB__ConnectionString environment variable.");
            }
            return new MongoClient(settings.ConnectionString);
        });

        services.AddSingleton<MongoContext>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IMusicRepository, MusicRepository>();
        services.AddScoped<IComposerRepository, ComposerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
