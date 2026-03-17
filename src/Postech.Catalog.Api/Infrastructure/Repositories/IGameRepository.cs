using Postech.Catalog.Api.Domain.Entities;

namespace Postech.Catalog.Api.Infrastructure.Repositories;

public interface IGameRepository
{
    Task<IEnumerable<Game?>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Game game, CancellationToken cancellationToken = default);
    Task UpdateAsync(Game game, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}