using MyMusic.Core.Models;

namespace MyMusic.Core.Services;

public interface IComposerService
{
    Task<IReadOnlyList<Composer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Composer?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Composer> CreateAsync(string firstName, string lastName, CancellationToken cancellationToken = default);
    Task<Composer?> UpdateAsync(string id, string firstName, string lastName, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
