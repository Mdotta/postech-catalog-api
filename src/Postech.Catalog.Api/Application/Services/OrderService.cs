using ErrorOr;
using Postech.Catalog.Api.Application.Events;
using Postech.Catalog.Api.Domain.Entities;
using Postech.Catalog.Api.Domain.Errors;
using Postech.Catalog.Api.Infrastructure.Messaging;
using Postech.Catalog.Api.Infrastructure.Repositories;

namespace Postech.Catalog.Api.Application.Services;

public class OrderService(
    ILogger<OrderService> logger, 
    IEventPublisher publisher,
    IGameRepository gameRepository,
    IOrderRepository orderRepository):IOrderService
{
    public async Task<ErrorOr<Success>> PlaceOrder(Guid userId, Guid gameId)
    {
        // Here you would typically have logic to create an order in your database
        // For this example, we'll just publish an event to indicate that an order has been placed
        var game = await gameRepository.GetByIdAsync(gameId);
        if (game == null)
        {
            logger.LogError($"Game with id {gameId} does not exist");
            return Errors.Game.NotFound;
        }

        var order = new Order()
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
            OrderId = Guid.NewGuid(),
            TotalAmount = 59.99m,
            PlacedAt = DateTime.UtcNow
        };
        
        await publisher.PublishAsync(orderPlacedEvent);

        return Result.Success;
    }
}