namespace CatalogAPI.Interfaces;

/// <summary>
/// Serviço de pedidos. Usa FILA: publica OrderPlacedEvent para processamento assíncrono pelo PaymentService.
/// A API retorna Accepted (202) imediatamente — o pagamento é processado em background.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Cria pedido e publica na fila (OrderPlacedEvent).
    /// O cliente recebe 202 Accepted — não espera o resultado do pagamento.
    /// </summary>
    Task<(Guid OrderId, bool Success, string? Error)> PlaceOrderAsync(Guid userId, Guid gameId, CancellationToken ct = default);
}
