using MyMusic.Core.Models;

namespace MyMusic.Core.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Returns false if the username is already taken.</summary>
    Task<bool> TryCreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets only the given fields (the password hash only when not null), so
    /// overlapping updates can't restore each other's stale values. Returns
    /// the updated user, or null if not found.
    /// </summary>
    Task<User?> UpdateProfileAsync(string id, string firstName, string lastName, string? passwordHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the password hash only if it still equals <paramref name="expectedHash"/>,
    /// so a rehash on login never undoes a password change made meanwhile.
    /// </summary>
    Task<bool> ReplacePasswordHashAsync(string id, string expectedHash, string newHash, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
