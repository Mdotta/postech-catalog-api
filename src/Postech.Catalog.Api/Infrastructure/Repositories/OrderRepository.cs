using Microsoft.EntityFrameworkCore;
using Postech.Catalog.Api.Domain.Entities;
using Postech.Catalog.Api.Domain.Enums;
using Postech.Catalog.Api.Infrastructure.Data;

namespace Postech.Catalog.Api.Infrastructure.Repositories;

public class OrderRepository(CatalogDbContext context): IOrderRepository
{
    public async Task<IEnumerable<Order?>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == id, cancellationToken: cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await context.Orders.AddAsync(order, cancellationToken);
        await  context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == OrderStatus.Completed)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        context.Orders.Update(order);
        await context.SaveChangesAsync(cancellationToken);
    }
}