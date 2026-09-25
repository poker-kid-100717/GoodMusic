using MongoDB.Driver;
using MyMusic.Core.Models;
using MyMusic.Core.Repositories;

namespace MyMusic.Mongo.Db.Repositories;

public class UserRepository(MongoContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(id)
            ? await context.Users.Find(u => u.Id == id).FirstOrDefaultAsync(cancellationToken)
            : null;

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        await context.Users.Find(u => u.Username == username).FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryCreateAsync(User user, CancellationToken cancellationToken = default)
    {
        // The unique index on username decides races between concurrent sign-ups.
        try
        {
            await context.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task<User?> UpdateProfileAsync(string id, string firstName, string lastName, string? passwordHash, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(id)) return null;

        var update = Builders<User>.Update.Set(u => u.FirstName, firstName).Set(u => u.LastName, lastName);
        if (passwordHash is not null)
        {
            update = update.Set(u => u.PasswordHash, passwordHash);
        }

        return await context.Users.FindOneAndUpdateAsync(
            u => u.Id == id, update,
            new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After }, cancellationToken);
    }

    public async Task<bool> ReplacePasswordHashAsync(string id, string expectedHash, string newHash, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(id)) return false;
        var result = await context.Users.UpdateOneAsync(
            u => u.Id == id && u.PasswordHash == expectedHash,
            Builders<User>.Update.Set(u => u.PasswordHash, newHash),
            cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(id)) return false;
        var result = await context.Users.DeleteOneAsync(u => u.Id == id, cancellationToken);
        return result.DeletedCount > 0;
    }
}
