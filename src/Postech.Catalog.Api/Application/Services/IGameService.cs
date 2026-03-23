using ErrorOr;
using Postech.Catalog.Api.Application.DTOs;

namespace Postech.Catalog.Api.Application.Services;

public interface IGameService
{
    Task<ErrorOr<List<GameResponse>>> GetAllGamesAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<GameResponse>> GetGameByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> CreateGameAsync(CreateGameRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> UpdateGameAsync(UpdateGameRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> DeleteGameAsync(Guid id, CancellationToken cancellationToken = default);
}
