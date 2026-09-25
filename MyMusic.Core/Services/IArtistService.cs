using MyMusic.Core.Models;

namespace MyMusic.Core.Services;

public interface IArtistService
{
    Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Artist?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Artist> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<ServiceResult<Artist>> RenameAsync(string id, string name, CancellationToken cancellationToken = default);
    Task<ServiceResult<Artist>> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
