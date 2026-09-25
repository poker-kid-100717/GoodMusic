using MyMusic.Core.Models;

namespace MyMusic.Core.Services;

public interface IMusicService
{
    Task<IReadOnlyList<Music>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Music>> GetByArtistIdAsync(string artistId, CancellationToken cancellationToken = default);
    Task<Music?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ServiceResult<Music>> CreateAsync(string name, string artistId, CancellationToken cancellationToken = default);
    Task<ServiceResult<Music>> UpdateAsync(string id, string name, string artistId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
