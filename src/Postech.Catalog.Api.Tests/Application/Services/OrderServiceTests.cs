using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Postech.Catalog.Api.Application.Services;
using Postech.Catalog.Api.Domain.Entities;
using Postech.Catalog.Api.Domain.Errors;
using Postech.Catalog.Api.Infrastructure.Messaging;
using Postech.Catalog.Api.Infrastructure.Repositories;
using Postech.Shared.Contracts.Events;

namespace Postech.Catalog.Api.Tests.Application.Services;

public class OrderServiceTests
{
    private readonly ILogger<OrderService> _logger = Substitute.For<ILogger<OrderService>>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly IGameRepository _gameRepository = Substitute.For<IGameRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();

    private OrderService CreateSut() => new(_logger, _publisher, _gameRepository, _orderRepository);

    [Fact]
    public async Task PlaceOrder_WhenGameMissing_ReturnsNotFound()
    {
        var gameId = Guid.NewGuid();
        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await CreateSut().PlaceOrder(Guid.NewGuid(), gameId);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.Game.NotFound);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_WhenGameExists_PersistsOrderAndPublishesOrderPlacedEvent_WithSameOrderIdAndPrice()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = new Game("G", "D", 49.99m, "RPG", DateTime.UtcNow) { Id = gameId };
        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);
        _orderRepository.AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await CreateSut().PlaceOrder(userId, gameId);

        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeEmpty();

        await _orderRepository.Received(1).AddAsync(
            Arg.Is<Order>(o =>
                o.OrderId == result.Value &&
                o.UserId == userId &&
                o.GameId == gameId &&
                o.TotalAmount == 49.99m),
            Arg.Any<CancellationToken>());

        await _publisher.Received(1).PublishAsync(
            Arg.Is<OrderPlacedEvent>(e =>
                e.OrderId == result.Value &&
                e.UserId == userId &&
                e.GameId == gameId &&
                e.TotalAmount == 49.99m),
            Arg.Any<CancellationToken>());
    }
}
