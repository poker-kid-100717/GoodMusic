using MongoDB.Driver;
using MyMusic.Core.Models;
using MyMusic.Core.Repositories;

namespace MyMusic.Mongo.Db.Repositories;

public class ArtistRepository(MongoContext context) : IArtistRepository
{
    public async Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Artists.Find(FilterDefinition<Artist>.Empty).SortBy(a => a.Name).ToListAsync(cancellationToken);

    public async Task<Artist?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(id)
            ? await context.Artists.Find(a => a.Id == id).FirstOrDefaultAsync(cancellationToken)
            : null;

    public Task CreateAsync(Artist artist, CancellationToken cancellationToken = default) =>
        context.Artists.InsertOneAsync(artist, cancellationToken: cancellationToken);

    public async Task<bool> UpdateAsync(Artist artist, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(artist.Id)) return false;
        var result = await context.Artists.ReplaceOneAsync(a => a.Id == artist.Id, artist, cancellationToken: cancellationToken);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(id)) return false;
        var result = await context.Artists.DeleteOneAsync(a => a.Id == id, cancellationToken);
        return result.DeletedCount > 0;
    }
}
