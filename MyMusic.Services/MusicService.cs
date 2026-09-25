using MyMusic.Core.Models;
using MyMusic.Core.Repositories;
using MyMusic.Core.Services;

namespace MyMusic.Services;

/// <summary>
/// Songs reference an artist and carry a copy of its name. Every write that
/// points a song at an artist first claims it with an atomic counter
/// increment (which fails if the artist was deleted), and afterwards
/// reconciles the name copy in case the artist was renamed meanwhile.
/// </summary>
public class MusicService(IMusicRepository musics, IArtistRepository artists) : IMusicService
{
    public Task<IReadOnlyList<Music>> GetAllAsync(CancellationToken cancellationToken = default) =>
        musics.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<Music>> GetByArtistIdAsync(string artistId, CancellationToken cancellationToken = default) =>
        musics.GetByArtistIdAsync(artistId, cancellationToken);

    public Task<Music?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        musics.GetByIdAsync(id, cancellationToken);

    public async Task<ServiceResult<Music>> CreateAsync(string name, string artistId, CancellationToken cancellationToken = default)
    {
        var artist = await artists.AddSongReferenceAsync(artistId, cancellationToken);
        if (artist is null)
        {
            return ServiceResult<Music>.Invalid($"Artist '{artistId}' does not exist.");
        }

        var music = new Music { Name = name.Trim(), ArtistId = artist.Id, ArtistName = artist.Name };
        try
        {
            await musics.CreateAsync(music, cancellationToken);
        }
        catch
        {
            await artists.RemoveSongReferenceAsync(artist.Id, CancellationToken.None);
            throw;
        }

        await ReconcileArtistNameAsync(music, cancellationToken);
        return ServiceResult<Music>.Ok(music);
    }

    public async Task<ServiceResult<Music>> UpdateAsync(string id, string name, string artistId, CancellationToken cancellationToken = default)
    {
        var music = await musics.GetByIdAsync(id, cancellationToken);
        if (music is null)
        {
            return ServiceResult<Music>.NotFound();
        }

        var previousArtistId = music.ArtistId;
        var artistChanged = previousArtistId != artistId;

        var artist = artistChanged
            ? await artists.AddSongReferenceAsync(artistId, cancellationToken)
            : await artists.GetByIdAsync(artistId, cancellationToken);
        if (artist is null)
        {
            return ServiceResult<Music>.Invalid($"Artist '{artistId}' does not exist.");
        }

        music.Name = name.Trim();
        music.ArtistId = artist.Id;
        music.ArtistName = artist.Name;
        await musics.UpdateAsync(music, cancellationToken);

        if (artistChanged)
        {
            await artists.RemoveSongReferenceAsync(previousArtistId, cancellationToken);
        }

        await ReconcileArtistNameAsync(music, cancellationToken);
        return ServiceResult<Music>.Ok(music);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var music = await musics.GetByIdAsync(id, cancellationToken);
        if (music is null || !await musics.DeleteAsync(id, cancellationToken))
        {
            return false;
        }

        await artists.RemoveSongReferenceAsync(music.ArtistId, cancellationToken);
        return true;
    }

    /// <summary>
    /// If the artist was renamed between reading its name and writing the song,
    /// the rename's bulk update may have run before this song existed. Re-read
    /// and fix the copy; a rename landing after this re-read will still reach
    /// the song through its own bulk update, because the song now exists.
    /// </summary>
    private async Task ReconcileArtistNameAsync(Music music, CancellationToken cancellationToken)
    {
        var current = await artists.GetByIdAsync(music.ArtistId, cancellationToken);
        if (current is not null && current.Name != music.ArtistName)
        {
            music.ArtistName = current.Name;
            await musics.UpdateAsync(music, cancellationToken);
        }
    }
}
