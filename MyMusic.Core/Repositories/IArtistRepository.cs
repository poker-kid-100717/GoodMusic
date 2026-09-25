using MyMusic.Core.Models;

namespace MyMusic.Core.Repositories;

public enum ArtistDeleteOutcome
{
    Deleted,
    NotFound,
    HasSongs
}

public interface IArtistRepository
{
    Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Artist?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task CreateAsync(Artist artist, CancellationToken cancellationToken = default);

    /// <summary>Sets the name only (leaves the song counter alone). Null if not found.</summary>
    Task<Artist?> RenameAsync(string id, string name, CancellationToken cancellationToken = default);

    /// <summary>Counts one more song for the artist. Call inside the transaction that writes the song.</summary>
    Task AddSongReferenceAsync(string id, CancellationToken cancellationToken = default);

    Task RemoveSongReferenceAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the artist only if no songs reference it, in one atomic operation.</summary>
    Task<ArtistDeleteOutcome> DeleteIfUnusedAsync(string id, CancellationToken cancellationToken = default);
}
