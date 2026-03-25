using ErrorOr;
using Postech.Catalog.Api.Application.DTOs;

namespace Postech.Catalog.Api.Application.Services;

public interface IOrderService
{
    Task<ErrorOr<Guid>> PlaceOrder(Guid userId, Guid gameId);
    Task<ErrorOr<List<GameResponse>>> GetUserLibraryAsync(Guid userId, CancellationToken cancellationToken = default);
}