using Postech.Catalog.Api.Infrastructure.MongoDB.Documents;

namespace Postech.Catalog.Api.Infrastructure.Repositories;

public interface IGameDocumentRepository
{
    Task<GameDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpsertAsync(GameDocument document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
