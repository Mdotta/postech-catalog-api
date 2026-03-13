using CatalogAPI.Data;
using CatalogAPI.Interfaces;
using CatalogAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Services;

public class GameService(AppDbContext db) : IGameService
{
    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Games.ToListAsync(ct);
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Games.FindAsync([id], ct);
    }

    public async Task<Game> CreateAsync(string title, string description, string developer, string publisher, decimal price, CancellationToken ct = default)
    {
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Developer = developer,
            Publisher = publisher,
            Price = price,
            CreatedAt = DateTime.UtcNow
        };
        db.Games.Add(game);
        await db.SaveChangesAsync(ct);
        return game;
    }

    public async Task<Game?> UpdateAsync(Guid id, string? title, string? description, string? developer, string? publisher, decimal? price, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([id], ct);
        if (game is null) return null;

        if (title != null) game.Title = title;
        if (description != null) game.Description = description;
        if (developer != null) game.Developer = developer;
        if (publisher != null) game.Publisher = publisher;
        if (price.HasValue) game.Price = price.Value;

        await db.SaveChangesAsync(ct);
        return game;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([id], ct);
        if (game is null) return false;

        db.Games.Remove(game);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
