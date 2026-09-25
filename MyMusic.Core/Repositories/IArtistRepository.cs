using MyMusic.Core.Models;

namespace MyMusic.Core.Repositories;

public interface IArtistRepository
{
    Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Artist?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task CreateAsync(Artist artist, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Artist artist, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
