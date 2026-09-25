using MongoDB.Driver;
using MyMusic.Core.Models;
using MyMusic.Core.Repositories;

namespace MyMusic.Mongo.Db.Repositories;

public class MusicRepository(MongoContext context, MongoSession session) : IMusicRepository
{
    public async Task<IReadOnlyList<Music>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Musics.Find(session.Handle, FilterDefinition<Music>.Empty).SortBy(m => m.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Music>> GetByArtistIdAsync(string artistId, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(artistId)
            ? await context.Musics.Find(session.Handle, m => m.ArtistId == artistId).SortBy(m => m.Name).ToListAsync(cancellationToken)
            : [];

    public async Task<Music?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(id)
            ? await context.Musics.Find(session.Handle, m => m.Id == id).FirstOrDefaultAsync(cancellationToken)
            : null;

    public async Task<bool> AnyByArtistIdAsync(string artistId, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(artistId) && await context.Musics.Find(session.Handle, m => m.ArtistId == artistId).AnyAsync(cancellationToken);

    public Task CreateAsync(Music music, CancellationToken cancellationToken = default) =>
        context.Musics.InsertOneAsync(session.Handle, music, cancellationToken: cancellationToken);

    public async Task<bool> UpdateAsync(Music music, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(music.Id)) return false;
        var result = await context.Musics.ReplaceOneAsync(session.Handle, m => m.Id == music.Id, music, cancellationToken: cancellationToken);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(id)) return false;
        var result = await context.Musics.DeleteOneAsync(session.Handle, m => m.Id == id, cancellationToken: cancellationToken);
        return result.DeletedCount > 0;
    }

    public Task RenameArtistAsync(string artistId, string artistName, CancellationToken cancellationToken = default) =>
        !MongoContext.IsValidId(artistId) ? Task.CompletedTask : context.Musics.UpdateManyAsync(
            session.Handle,
            m => m.ArtistId == artistId,
            Builders<Music>.Update.Set(m => m.ArtistName, artistName),
            cancellationToken: cancellationToken);
}
