using MyMusic.Core.Models;

namespace MyMusic.Core.Repositories;

public interface IMusicRepository
{
    Task<IReadOnlyList<Music>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Music>> GetByArtistIdAsync(string artistId, CancellationToken cancellationToken = default);
    Task<Music?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> AnyByArtistIdAsync(string artistId, CancellationToken cancellationToken = default);
    Task CreateAsync(Music music, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Music music, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task RenameArtistAsync(string artistId, string artistName, CancellationToken cancellationToken = default);
}
