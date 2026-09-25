using MyMusic.Core.Models;

namespace MyMusic.Core.Repositories;

public interface IComposerRepository
{
    Task<IReadOnlyList<Composer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Composer?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task CreateAsync(Composer composer, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Composer composer, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
