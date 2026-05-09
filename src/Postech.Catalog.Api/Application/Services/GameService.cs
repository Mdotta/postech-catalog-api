using ErrorOr;
using Postech.Catalog.Api.Application.DTOs;
using Postech.Catalog.Api.Domain.Entities;
using Postech.Catalog.Api.Domain.Errors;
using Postech.Catalog.Api.Infrastructure.Cache;
using Postech.Catalog.Api.Infrastructure.Messaging;
using Postech.Catalog.Api.Infrastructure.MongoDB.Documents;
using Postech.Catalog.Api.Infrastructure.MongoDB.Repositories;
using Postech.Catalog.Api.Infrastructure.Repositories;

namespace Postech.Catalog.Api.Application.Services;

public class GameService: IGameService
{
    private const string AllGamesCacheKey = "catalog:games:all";

    private readonly IGameRepository _gameRepository;
    private readonly IGameMongoRepository? _gameMongoRepository;
    private readonly ICacheService? _cacheService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<GameService> _logger;
    
    public GameService(IGameRepository gameRepository,
        IEventPublisher eventPublisher,
        ILogger<GameService> logger,
        IGameMongoRepository? gameMongoRepository = null,
        ICacheService? cacheService = null)
    {
        _gameRepository = gameRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _gameMongoRepository = gameMongoRepository;
        _cacheService = cacheService;
    }
    
    public async Task<ErrorOr<List<GameResponse>>> GetAllGamesAsync(CancellationToken cancellationToken = default)
    {
        if (_cacheService is not null)
        {
            var cached = await _cacheService.GetAsync<List<GameResponse>>(AllGamesCacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogInformation("Cache hit: {Key}", AllGamesCacheKey);
                return cached;
            }
        }

        var games = await _gameRepository.GetAllAsync(cancellationToken);
        
        var response = games.Select(game => new GameResponse(
            game.Id,
            game.Name,
            game.Description,
            game.Price,
            game.Genre,
            game.ReleaseDate
        )).ToList();

        if (_cacheService is not null)
            await _cacheService.SetAsync(AllGamesCacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

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
        var game = new Game(
            request.Name,
            request.Description,
            request.Price,
            request.Genre,
            request.ReleaseDate
        );
        
        await _gameRepository.AddAsync(game, cancellationToken);

        if (_gameMongoRepository is not null)
        {
            try
            {
                await _gameMongoRepository.UpsertAsync(ToDocument(game, request), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB sync failed for game {GameId}. Postgres record is intact.", game.Id);
            }
        }

        if (_cacheService is not null)
            await _cacheService.RemoveAsync(AllGamesCacheKey, cancellationToken);

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
            game.UpdateReleaseDate(request.ReleaseDate.Value);
            changed = true;
        }
        
        if (changed)
        {
            await _gameRepository.UpdateAsync(game, cancellationToken);

            if (_gameMongoRepository is not null)
            {
                try
                {
                    var existing = await _gameMongoRepository.GetByIdAsync(game.Id, cancellationToken);
                    await _gameMongoRepository.UpsertAsync(ToDocument(game, request, existing), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MongoDB sync failed for game {GameId}. Postgres record is intact.", game.Id);
                }
            }

            if (_cacheService is not null)
                await _cacheService.RemoveAsync(AllGamesCacheKey, cancellationToken);
        }

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

        if (_gameMongoRepository is not null)
        {
            try
            {
                await _gameMongoRepository.DeleteAsync(id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB sync failed on delete for game {GameId}. Postgres record is intact.", id);
            }
        }

        if (_cacheService is not null)
            await _cacheService.RemoveAsync(AllGamesCacheKey, cancellationToken);

        return Result.Success;
    }

    private static GameDocument ToDocument(Game game, CreateGameRequest request) => new()
    {
        Id = game.Id,
        Name = game.Name,
        Description = game.Description,
        Genre = game.Genre,
        Price = game.Price,
        ReleaseDate = game.ReleaseDate,
        CreatedAt = game.CreatedAt,
        UpdatedAt = DateTime.UtcNow,
        Tags = request.Tags ?? [],
        Screenshots = request.Screenshots ?? [],
        Developer = request.Developer ?? string.Empty,
        Publisher = request.Publisher ?? string.Empty
    };

    private static GameDocument ToDocument(Game game, UpdateGameRequest request, GameDocument? existing) => new()
    {
        Id = game.Id,
        Name = game.Name,
        Description = game.Description,
        Genre = game.Genre,
        Price = game.Price,
        ReleaseDate = game.ReleaseDate,
        CreatedAt = existing?.CreatedAt ?? game.CreatedAt,
        UpdatedAt = DateTime.UtcNow,
        // Se o request trouxer os campos expandidos, usa os novos; senão preserva o que já estava no MongoDB
        Tags = request.Tags ?? existing?.Tags ?? [],
        Screenshots = request.Screenshots ?? existing?.Screenshots ?? [],
        Developer = request.Developer ?? existing?.Developer ?? string.Empty,
        Publisher = request.Publisher ?? existing?.Publisher ?? string.Empty
    };
}