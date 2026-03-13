using CatalogAPI.Models;

namespace CatalogAPI.Interfaces;

public interface IGameService
{
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default);
    Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Game> CreateAsync(string title, string description, string developer, string publisher, decimal price, CancellationToken ct = default);
    Task<Game?> UpdateAsync(Guid id, string? title, string? description, string? developer, string? publisher, decimal? price, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
