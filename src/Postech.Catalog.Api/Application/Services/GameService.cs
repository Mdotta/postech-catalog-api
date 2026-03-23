using ErrorOr;
using Postech.Catalog.Api.Application.DTOs;
using Postech.Catalog.Api.Domain.Entities;
using Postech.Catalog.Api.Domain.Errors;
using Postech.Catalog.Api.Infrastructure.Messaging;
using Postech.Catalog.Api.Infrastructure.Repositories;

namespace Postech.Catalog.Api.Application.Services;

public class GameService: IGameService
{
    
    private readonly IGameRepository _gameRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<GameService> _logger;
    
    public GameService(IGameRepository gameRepository,
        IEventPublisher eventPublisher,
        ILogger<GameService> logger)
    {
        _gameRepository = gameRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }
    
    public async Task<ErrorOr<List<GameResponse>>> GetAllGamesAsync(CancellationToken cancellationToken = default)
    {
        var games = await _gameRepository.GetAllAsync(cancellationToken);
        
        var response = games
            .Where(game => game is not null)
            .Select(game => new GameResponse(
                game!.Id,
                game.Name,
                game.Description,
                game.Price,
                game.Genre,
                game.ReleaseDate
            ))
            .ToList();

        return response;
    }
    
    public async Task<ErrorOr<GameResponse>> GetGameByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken);
        
        if (game == null)
        {
            _logger.LogWarning("Game with ID {GameId} not found", id);
            return Errors.Game.NotFound;
        }

        var response = new GameResponse(
            game.Id,
            game.Name,
            game.Description,
            game.Price,
            game.Genre,
            game.ReleaseDate
        );

        return response;
    }
    
    public async Task<ErrorOr<Success>> CreateGameAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        var releaseDateUtc = DateTime.SpecifyKind(
            request.ReleaseDate.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var game = new Game(
            request.Name,
            request.Description,
            request.Price,
            request.Genre,
            releaseDateUtc
        );
        
        await _gameRepository.AddAsync(game, cancellationToken);

        return Result.Success;
    }
    
    public async Task<ErrorOr<Success>> UpdateGameAsync(UpdateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (game == null)
        {
            _logger.LogWarning("Game with ID {GameId} not found for update", request.Id);
            return Errors.Game.NotFound;
        }

        bool changed = false;
        if (!string.IsNullOrEmpty(request.Name))
        {
            game.UpdateName(request.Name);
            changed = true;
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            game.UpdateDescription(request.Description);
            changed = true;
        }

        if (request.Price.HasValue)
        {
            game.UpdatePrice(request.Price.Value);
            changed = true;
        }

        if (!string.IsNullOrEmpty(request.Genre))
        {
            game.UpdateGenre(request.Genre);
            changed = true;           
        }

        if (request.ReleaseDate.HasValue)
        {
            var releaseDateUtc = DateTime.SpecifyKind(
                request.ReleaseDate.Value.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
            game.UpdateReleaseDate(releaseDateUtc);
            changed = true;
        }
        
        if (changed)
            await _gameRepository.UpdateAsync(game, cancellationToken);

        return Result.Success;
    }
    
    public async Task<ErrorOr<Success>> DeleteGameAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken);
        
        if (game == null)
        {
            _logger.LogWarning("Game with ID {GameId} not found for deletion", id);
            return Errors.Game.NotFound;
        }
        
        await _gameRepository.DeleteAsync(id, cancellationToken);

        return Result.Success;
    }
    
    
    
}