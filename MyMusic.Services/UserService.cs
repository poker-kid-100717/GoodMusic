using Microsoft.AspNetCore.Identity;
using MyMusic.Core.Models;
using MyMusic.Core.Repositories;
using MyMusic.Core.Services;

namespace MyMusic.Services;

public class UserService(IUserRepository users, IPasswordHasher<User> passwordHasher) : IUserService
{
    public async Task<ServiceResult<User>> RegisterAsync(string username, string password, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Username = Normalize(username),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim()
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        return await users.TryCreateAsync(user, cancellationToken)
            ? ServiceResult<User>.Ok(user)
            : ServiceResult<User>.Conflict($"Username '{user.Username}' is already taken.");
    }

    public async Task<User?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByUsernameAsync(Normalize(username), cancellationToken);
        if (user is null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            await users.ReplacePasswordHashAsync(user.Id, user.PasswordHash, passwordHasher.HashPassword(user, password), cancellationToken);
        }

        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        users.GetByIdAsync(id, cancellationToken);

    public Task<User?> UpdateAsync(string id, string firstName, string lastName, string? newPassword, CancellationToken cancellationToken = default)
    {
        // PasswordHasher<TUser> doesn't read the user; a placeholder is enough.
        var passwordHash = string.IsNullOrEmpty(newPassword) ? null : passwordHasher.HashPassword(new User { Id = id }, newPassword);
        return users.UpdateProfileAsync(id, firstName.Trim(), lastName.Trim(), passwordHash, cancellationToken);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        users.DeleteAsync(id, cancellationToken);

    private static string Normalize(string username) => username.Trim().ToLowerInvariant();
}
