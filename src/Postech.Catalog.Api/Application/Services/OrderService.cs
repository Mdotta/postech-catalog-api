using ErrorOr;
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
    IOrderRepository orderRepository):IOrderService
{
    public async Task<ErrorOr<Guid>> PlaceOrder(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await gameRepository.GetByIdAsync(gameId, cancellationToken);
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
        await orderRepository.AddAsync(order, cancellationToken);

        var orderPlacedEvent = new OrderPlacedEvent
        {
            OrderId = order.OrderId,
            UserId = userId,
            GameId = gameId,
            TotalAmount = order.TotalAmount,
            PlacedAt = order.PlacedAt
        };

        await publisher.PublishAsync(orderPlacedEvent, cancellationToken);

        return order.OrderId;
    }
}