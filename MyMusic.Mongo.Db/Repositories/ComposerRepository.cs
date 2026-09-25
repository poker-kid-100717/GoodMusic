using MongoDB.Driver;
using MyMusic.Core.Models;
using MyMusic.Core.Repositories;

namespace MyMusic.Mongo.Db.Repositories;

public class ComposerRepository(MongoContext context) : IComposerRepository
{
    public async Task<IReadOnlyList<Composer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Composers.Find(FilterDefinition<Composer>.Empty)
            .SortBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken);

    public async Task<Composer?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        MongoContext.IsValidId(id)
            ? await context.Composers.Find(c => c.Id == id).FirstOrDefaultAsync(cancellationToken)
            : null;

    public Task CreateAsync(Composer composer, CancellationToken cancellationToken = default) =>
        context.Composers.InsertOneAsync(composer, cancellationToken: cancellationToken);

    public async Task<bool> UpdateAsync(Composer composer, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(composer.Id)) return false;
        var result = await context.Composers.ReplaceOneAsync(c => c.Id == composer.Id, composer, cancellationToken: cancellationToken);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!MongoContext.IsValidId(id)) return false;
        var result = await context.Composers.DeleteOneAsync(c => c.Id == id, cancellationToken);
        return result.DeletedCount > 0;
    }
}
