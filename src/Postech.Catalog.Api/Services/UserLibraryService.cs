using CatalogAPI.Data;
using CatalogAPI.Interfaces;
using CatalogAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Services;

public class UserLibraryService(AppDbContext db) : IUserLibraryService
{
    public async Task<IReadOnlyList<UserLibraryItemDto>> GetLibraryAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.UserLibraryItems
            .Where(x => x.UserId == userId)
            .Join(db.Games, lib => lib.GameId, g => g.Id, (lib, g) => new UserLibraryItemDto(lib.AddedAt, g.Id, g.Title))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Chamado pelo PaymentProcessedConsumer (FILA) — não pela API.
    /// </summary>
    public async Task<bool> AddToLibraryAsync(Guid orderId, Guid userId, Guid gameId, CancellationToken ct = default)
    {
        var exists = await db.UserLibraryItems.AnyAsync(x => x.OrderId == orderId, ct);
        if (exists) return false;

        db.UserLibraryItems.Add(new UserLibraryItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            OrderId = orderId,
            AddedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return true;
    }
}
