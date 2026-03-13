using CatalogAPI.Events;
using CatalogAPI.Interfaces;
using MassTransit;

namespace CatalogAPI.Consumers;

/// <summary>
/// FILA: Consumidor do PaymentProcessedEvent (publicado pelo PaymentService).
/// Quando o pagamento é aprovado, adiciona o jogo à biblioteca via UserLibraryService.
/// NÃO é chamado por API HTTP — apenas por mensagens RabbitMQ.
/// </summary>
public class PaymentProcessedConsumer(IUserLibraryService libraryService, ILogger<PaymentProcessedConsumer> log) : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var msg = context.Message;
        log.LogInformation("Processando PaymentProcessedEvent OrderId={OrderId}, UserId={UserId}, GameId={GameId}, Status={Status}",
            msg.OrderId, msg.UserId, msg.GameId, msg.Status);

        if (msg.Status != "Approved") return;

        var added = await libraryService.AddToLibraryAsync(msg.OrderId, msg.UserId, msg.GameId, context.CancellationToken);
        if (added)
            log.LogInformation("Jogo {GameId} adicionado à biblioteca do usuário {UserId}", msg.GameId, msg.UserId);
    }
}
