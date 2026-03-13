namespace CatalogAPI.Events;

public record PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string Status { get; init; } = "";

    public PaymentProcessedEvent() { }

    public PaymentProcessedEvent(Guid orderId, Guid userId, Guid gameId, string status)
    {
        OrderId = orderId;
        UserId = userId;
        GameId = gameId;
        Status = status;
    }
}
