using Postech.Catalog.Api.Domain.Entities;

namespace Postech.Catalog.Api.Infrastructure.Repositories;

public interface IOrderRepository
{
    Task<IEnumerable<Order?>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}