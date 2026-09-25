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

    public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(user.Id)) return false;
        var result = await context.Users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(id)) return false;
        var result = await context.Users.DeleteOneAsync(u => u.Id == id, cancellationToken);
        return result.DeletedCount > 0;
    }
}
