using MongoDB.Driver;
using MyMusic.Core.Models;
using MyMusic.Core.Repositories;

namespace MyMusic.Mongo.Db.Repositories;

public class ArtistRepository(MongoContext context, MongoSession session) : IArtistRepository
{
    private static readonly FindOneAndUpdateOptions<Artist> ReturnUpdated = new() { ReturnDocument = ReturnDocument.After };

    public async Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Artists.Find(session.Handle, FilterDefinition<Artist>.Empty).SortBy(a => a.Name).ToListAsync(cancellationToken);

    public async Task<Artist?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(id)
            ? await context.Artists.Find(session.Handle, a => a.Id == id).FirstOrDefaultAsync(cancellationToken)
            : null;

    public Task CreateAsync(Artist artist, CancellationToken cancellationToken = default) =>
        context.Artists.InsertOneAsync(session.Handle, artist, cancellationToken: cancellationToken);

    public async Task<Artist?> RenameAsync(string id, string name, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(id)
            ? await context.Artists.FindOneAndUpdateAsync<Artist>(
                session.Handle, a => a.Id == id, Builders<Artist>.Update.Set(a => a.Name, name), ReturnUpdated, cancellationToken)
            : null;

    public Task AddSongReferenceAsync(string id, CancellationToken cancellationToken = default) =>
        context.Artists.UpdateOneAsync(
            session.Handle, a => a.Id == id, Builders<Artist>.Update.Inc(a => a.SongCount, 1),
            cancellationToken: cancellationToken);

    public Task RemoveSongReferenceAsync(string id, CancellationToken cancellationToken = default) =>
        context.Artists.UpdateOneAsync(
            session.Handle, a => a.Id == id && a.SongCount > 0, Builders<Artist>.Update.Inc(a => a.SongCount, -1),
            cancellationToken: cancellationToken);

    public async Task<ArtistDeleteOutcome> DeleteIfUnusedAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(id))
        {
            return ArtistDeleteOutcome.NotFound;
        }

        // The filter and the delete are one server-side operation. A song
        // transaction that counted this artist conflicts with it, so the two
        // can't both succeed.
        var result = await context.Artists.DeleteOneAsync(session.Handle, a => a.Id == id && a.SongCount == 0, cancellationToken: cancellationToken);
        if (result.DeletedCount > 0)
        {
            return ArtistDeleteOutcome.Deleted;
        }

        return await context.Artists.Find(session.Handle, a => a.Id == id).AnyAsync(cancellationToken)
            ? ArtistDeleteOutcome.HasSongs
            : ArtistDeleteOutcome.NotFound;
    }
}
