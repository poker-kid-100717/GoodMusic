using MongoDB.Driver;
using MyMusic.Core.Models;
using MyMusic.Core.Repositories;

namespace MyMusic.Mongo.Db.Repositories;

public class ArtistRepository(MongoContext context) : IArtistRepository
{
    private static readonly FindOneAndUpdateOptions<Artist> ReturnUpdated = new() { ReturnDocument = ReturnDocument.After };

    public async Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Artists.Find(FilterDefinition<Artist>.Empty).SortBy(a => a.Name).ToListAsync(cancellationToken);

    public async Task<Artist?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(id)
            ? await context.Artists.Find(a => a.Id == id).FirstOrDefaultAsync(cancellationToken)
            : null;

    public Task CreateAsync(Artist artist, CancellationToken cancellationToken = default) =>
        context.Artists.InsertOneAsync(artist, cancellationToken: cancellationToken);

    public async Task<Artist?> RenameAsync(string id, string name, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(id)
            ? await context.Artists.FindOneAndUpdateAsync<Artist>(
                a => a.Id == id, Builders<Artist>.Update.Set(a => a.Name, name), ReturnUpdated, cancellationToken)
            : null;

    public async Task<Artist?> AddSongReferenceAsync(string id, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(id)
            ? await context.Artists.FindOneAndUpdateAsync<Artist>(
                a => a.Id == id, Builders<Artist>.Update.Inc(a => a.SongCount, 1), ReturnUpdated, cancellationToken)
            : null;

    public Task RemoveSongReferenceAsync(string id, CancellationToken cancellationToken = default) =>
        !MongoContext.IsValidId(id)
            ? Task.CompletedTask
            : context.Artists.UpdateOneAsync(
                a => a.Id == id && a.SongCount > 0,
                Builders<Artist>.Update.Inc(a => a.SongCount, -1),
                cancellationToken: cancellationToken);

    public async Task<ArtistDeleteOutcome> DeleteIfUnusedAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(id))
        {
            return ArtistDeleteOutcome.NotFound;
        }

        // The filter and the delete are one server-side operation: a song
        // created concurrently has already incremented SongCount, or will find
        // the artist gone and be rejected.
        var result = await context.Artists.DeleteOneAsync(a => a.Id == id && a.SongCount == 0, cancellationToken);
        if (result.DeletedCount > 0)
        {
            return ArtistDeleteOutcome.Deleted;
        }

        return await context.Artists.Find(a => a.Id == id).AnyAsync(cancellationToken)
            ? ArtistDeleteOutcome.HasSongs
            : ArtistDeleteOutcome.NotFound;
    }
}
