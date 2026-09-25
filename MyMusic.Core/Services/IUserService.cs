using MyMusic.Core.Models;

namespace MyMusic.Core.Services;

public interface IUserService
{
    Task<ServiceResult<User>> RegisterAsync(string username, string password, string firstName, string lastName, CancellationToken cancellationToken = default);
    Task<User?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<User?> UpdateAsync(string id, string firstName, string lastName, string? newPassword, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
