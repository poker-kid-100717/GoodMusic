using MyMusic.Core.Models;
using MyMusic.Core.Repositories;
using MyMusic.Core.Services;

namespace MyMusic.Services;

public class ArtistService(IArtistRepository artists, IMusicRepository musics) : IArtistService
{
    public Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default) =>
        artists.GetAllAsync(cancellationToken);

    public Task<Artist?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        artists.GetByIdAsync(id, cancellationToken);

    public async Task<Artist> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var artist = new Artist { Name = name.Trim() };
        await artists.CreateAsync(artist, cancellationToken);
        return artist;
    }

    public async Task<ServiceResult<Artist>> RenameAsync(string id, string name, CancellationToken cancellationToken = default)
    {
        var artist = await artists.GetByIdAsync(id, cancellationToken);
        if (artist is null)
        {
            return ServiceResult<Artist>.NotFound();
        }

        artist.Name = name.Trim();
        await artists.UpdateAsync(artist, cancellationToken);

        // Songs carry a copy of the artist's name; keep it in step.
        await musics.RenameArtistAsync(artist.Id, artist.Name, cancellationToken);

        return ServiceResult<Artist>.Ok(artist);
    }

    public async Task<ServiceResult<Artist>> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var artist = await artists.GetByIdAsync(id, cancellationToken);
        if (artist is null)
        {
            return ServiceResult<Artist>.NotFound();
        }

        if (await musics.AnyByArtistIdAsync(id, cancellationToken))
        {
            return ServiceResult<Artist>.Conflict("The artist still has songs. Delete or reassign them first.");
        }

        await artists.DeleteAsync(id, cancellationToken);
        return ServiceResult<Artist>.Ok(artist);
    }
}
