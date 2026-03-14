namespace Postech.Catalog.Api.Application.Events;

public class OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime PlacedAt { get; init; }
}