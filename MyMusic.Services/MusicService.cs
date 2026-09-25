using MyMusic.Core.Models;
using MyMusic.Core.Repositories;
using MyMusic.Core.Services;

namespace MyMusic.Services;

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
        var artist = await artists.GetByIdAsync(artistId, cancellationToken);
        if (artist is null)
        {
            return ServiceResult<Music>.Invalid($"Artist '{artistId}' does not exist.");
        }

        var music = new Music { Name = name.Trim(), ArtistId = artist.Id, ArtistName = artist.Name };
        await musics.CreateAsync(music, cancellationToken);
        return ServiceResult<Music>.Ok(music);
    }

    public async Task<ServiceResult<Music>> UpdateAsync(string id, string name, string artistId, CancellationToken cancellationToken = default)
    {
        var music = await musics.GetByIdAsync(id, cancellationToken);
        if (music is null)
        {
            return ServiceResult<Music>.NotFound();
        }

        var artist = await artists.GetByIdAsync(artistId, cancellationToken);
        if (artist is null)
        {
            return ServiceResult<Music>.Invalid($"Artist '{artistId}' does not exist.");
        }

        music.Name = name.Trim();
        music.ArtistId = artist.Id;
        music.ArtistName = artist.Name;
        await musics.UpdateAsync(music, cancellationToken);
        return ServiceResult<Music>.Ok(music);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        musics.DeleteAsync(id, cancellationToken);
}
