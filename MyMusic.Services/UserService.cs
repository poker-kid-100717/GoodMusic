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
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            await users.UpdateAsync(user, cancellationToken);
        }

        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        users.GetByIdAsync(id, cancellationToken);

    public async Task<User?> UpdateAsync(string id, string firstName, string lastName, string? newPassword, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        if (!string.IsNullOrEmpty(newPassword))
        {
            user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        }

        await users.UpdateAsync(user, cancellationToken);
        return user;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        users.DeleteAsync(id, cancellationToken);

    private static string Normalize(string username) => username.Trim().ToLowerInvariant();
}
