using MyMusic.Core.Models;
using MyMusic.Core.Repositories;
using MyMusic.Core.Services;

namespace MyMusic.Services;

public class ArtistService(IArtistRepository artists, IMusicRepository musics, ITransactionRunner transactions) : IArtistService
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

    public Task<ServiceResult<Artist>> RenameAsync(string id, string name, CancellationToken cancellationToken = default) =>
        transactions.RunAsync(async ct =>
        {
            var artist = await artists.RenameAsync(id, name.Trim(), ct);
            if (artist is null)
            {
                return ServiceResult<Artist>.NotFound();
            }

            // Songs carry a copy of the artist's name; it changes in the same
            // transaction, so readers never see the two disagree.
            await musics.RenameArtistAsync(artist.Id, artist.Name, ct);
            return ServiceResult<Artist>.Ok(artist);
        }, cancellationToken);

    public async Task<ServiceResult<Artist>> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await artists.DeleteIfUnusedAsync(id, cancellationToken) switch
        {
            ArtistDeleteOutcome.Deleted => ServiceResult<Artist>.Ok(new Artist { Id = id }),
            ArtistDeleteOutcome.HasSongs => ServiceResult<Artist>.Conflict("The artist still has songs. Delete or reassign them first."),
            _ => ServiceResult<Artist>.NotFound()
        };
    }
}
