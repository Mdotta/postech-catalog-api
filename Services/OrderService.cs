using CatalogAPI.Events;
using CatalogAPI.Interfaces;
using MassTransit;

namespace CatalogAPI.Services;

/// <summary>
/// Usa FILA (RabbitMQ): publica OrderPlacedEvent para o PaymentService consumir.
/// NÃO faz chamada HTTP síncrona para pagamento — processamento assíncrono.
/// </summary>
public class OrderService(IGameService gameService, IPublishEndpoint publishEndpoint) : IOrderService
{
    public async Task<(Guid OrderId, bool Success, string? Error)> PlaceOrderAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        var game = await gameService.GetByIdAsync(gameId, ct);
        if (game is null)
            return (Guid.Empty, false, "Jogo não encontrado");

        var orderId = Guid.NewGuid();
        await publishEndpoint.Publish(new OrderPlacedEvent(orderId, userId, gameId, game.Price), ct);

        return (orderId, true, null);
    }
}
