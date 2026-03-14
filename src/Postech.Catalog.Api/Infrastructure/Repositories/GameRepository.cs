using Microsoft.EntityFrameworkCore;
using Postech.Catalog.Api.Domain.Entities;
using Postech.Catalog.Api.Infrastructure.Data;

namespace Postech.Catalog.Api.Infrastructure.Repositories;

public class GameRepository:IGameRepository
{

    private readonly CatalogDbContext _context;
    
    public GameRepository(CatalogDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<Game?>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
    }

    public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        await _context.Games.AddAsync(game, cancellationToken);
        await  _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await GetByIdAsync(id, cancellationToken);
        if (game != null)
        {
            _context.Games.Remove(game);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}