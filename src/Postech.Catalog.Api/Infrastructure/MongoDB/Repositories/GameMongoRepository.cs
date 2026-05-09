using MongoDB.Driver;
using Postech.Catalog.Api.Infrastructure.MongoDB.Documents;

namespace Postech.Catalog.Api.Infrastructure.MongoDB.Repositories;

public class GameMongoRepository : IGameMongoRepository
{
    private readonly IMongoCollection<GameDocument> _collection;

    public GameMongoRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<GameDocument>("games");
    }

    public async Task<GameDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GameDocument>.Filter.Eq(g => g.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(GameDocument document, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GameDocument>.Filter.Eq(g => g.Id, document.Id);
        var options = new ReplaceOptions { IsUpsert = true };
        await _collection.ReplaceOneAsync(filter, document, options, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GameDocument>.Filter.Eq(g => g.Id, id);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }
}
