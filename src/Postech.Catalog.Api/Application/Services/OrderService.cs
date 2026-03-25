using ErrorOr;
using Postech.Catalog.Api.Application.DTOs;
using Postech.Catalog.Api.Domain.Entities;
using Postech.Catalog.Api.Domain.Errors;
using Postech.Catalog.Api.Infrastructure.Messaging;
using Postech.Catalog.Api.Infrastructure.Repositories;
using Postech.Shared.Contracts.Events;

namespace Postech.Catalog.Api.Application.Services;

public class OrderService(
    ILogger<OrderService> logger,
    IEventPublisher publisher,
    IGameRepository gameRepository,
    IOrderRepository orderRepository) : IOrderService
{
    public async Task<ErrorOr<Guid>> PlaceOrder(Guid userId, Guid gameId)
    {
        var game = await gameRepository.GetByIdAsync(gameId);
        if (game == null)
        {
            logger.LogError("Game with id {GameId} does not exist", gameId);
            return Errors.Game.NotFound;
        }

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            TotalAmount = game.Price,
            PlacedAt = DateTime.UtcNow,
            Status = Domain.Enums.OrderStatus.Placed
        };
        await orderRepository.AddAsync(order);

        var orderPlacedEvent = new OrderPlacedEvent
        {
            GameId = gameId,
            UserId = userId,
            OrderId = order.OrderId,
            TotalAmount = game.Price,
            PlacedAt = order.PlacedAt
        };

        await publisher.PublishAsync(orderPlacedEvent);

        logger.LogInformation("Order {OrderId} placed for user {UserId}, game {GameId}", order.OrderId, userId, gameId);
        return order.OrderId;
    }

    public async Task<ErrorOr<List<GameResponse>>> GetUserLibraryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.GetByUserIdAsync(userId, cancellationToken);
        var games = new List<GameResponse>();

        foreach (var order in orders)
        {
            var game = await gameRepository.GetByIdAsync(order.GameId, cancellationToken);
            if (game is not null)
                games.Add(new GameResponse(game.Id, game.Name, game.Description, game.Price, game.Genre, game.ReleaseDate));
        }

        return games;
    }
}
