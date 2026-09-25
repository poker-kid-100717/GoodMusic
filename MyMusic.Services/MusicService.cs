using MyMusic.Core.Models;
using MyMusic.Core.Repositories;
using MyMusic.Core.Services;

namespace MyMusic.Services;

/// <summary>
/// Songs reference an artist, carry a copy of its name, and are counted on
/// it. Each write changes the song and the counters in one transaction, so a
/// concurrent rename, artist deletion or other song write either sees all of
/// it or none of it.
/// </summary>
public class MusicService(IMusicRepository musics, IArtistRepository artists, ITransactionRunner transactions) : IMusicService
{
    public Task<IReadOnlyList<Music>> GetAllAsync(CancellationToken cancellationToken = default) =>
        musics.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<Music>> GetByArtistIdAsync(string artistId, CancellationToken cancellationToken = default) =>
        musics.GetByArtistIdAsync(artistId, cancellationToken);

    public Task<Music?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        musics.GetByIdAsync(id, cancellationToken);

    public Task<ServiceResult<Music>> CreateAsync(string name, string artistId, CancellationToken cancellationToken = default) =>
        transactions.RunAsync(async ct =>
        {
            var artist = await artists.GetByIdAsync(artistId, ct);
            if (artist is null)
            {
                return ServiceResult<Music>.Invalid($"Artist '{artistId}' does not exist.");
            }

            var music = new Music { Name = name.Trim(), ArtistId = artist.Id, ArtistName = artist.Name };
            await musics.CreateAsync(music, ct);
            await artists.AddSongReferenceAsync(artist.Id, ct);
            return ServiceResult<Music>.Ok(music);
        }, cancellationToken);

    public Task<ServiceResult<Music>> UpdateAsync(string id, string name, string artistId, CancellationToken cancellationToken = default) =>
        transactions.RunAsync(async ct =>
        {
            var music = await musics.GetByIdAsync(id, ct);
            if (music is null)
            {
                return ServiceResult<Music>.NotFound();
            }

            var artist = await artists.GetByIdAsync(artistId, ct);
            if (artist is null)
            {
                return ServiceResult<Music>.Invalid($"Artist '{artistId}' does not exist.");
            }

            var previousArtistId = music.ArtistId;
            music.Name = name.Trim();
            music.ArtistId = artist.Id;
            music.ArtistName = artist.Name;
            await musics.UpdateAsync(music, ct);

            if (previousArtistId != artist.Id)
            {
                await artists.AddSongReferenceAsync(artist.Id, ct);
                await artists.RemoveSongReferenceAsync(previousArtistId, ct);
            }

            return ServiceResult<Music>.Ok(music);
        }, cancellationToken);

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        transactions.RunAsync(async ct =>
        {
            var music = await musics.GetByIdAsync(id, ct);
            if (music is null || !await musics.DeleteAsync(id, ct))
            {
                return false;
            }

            await artists.RemoveSongReferenceAsync(music.ArtistId, ct);
            return true;
        }, cancellationToken);
}
