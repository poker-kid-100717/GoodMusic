using MyMusic.Core.Models;
using MyMusic.Core.Repositories;
using MyMusic.Core.Services;

namespace MyMusic.Services;

public class ComposerService(IComposerRepository composers) : IComposerService
{
    public Task<IReadOnlyList<Composer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        composers.GetAllAsync(cancellationToken);

    public Task<Composer?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        composers.GetByIdAsync(id, cancellationToken);

    public async Task<Composer> CreateAsync(string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var composer = new Composer { FirstName = firstName.Trim(), LastName = lastName.Trim() };
        await composers.CreateAsync(composer, cancellationToken);
        return composer;
    }

    public async Task<Composer?> UpdateAsync(string id, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var composer = new Composer { Id = id, FirstName = firstName.Trim(), LastName = lastName.Trim() };
        return await composers.UpdateAsync(composer, cancellationToken) ? composer : null;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        composers.DeleteAsync(id, cancellationToken);
}
