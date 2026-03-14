namespace Postech.Catalog.Api.Application.Events;

public class OrderProcessedEvent
{
    public Guid OrderId { get; init; }
    public bool IsSuccessful { get; init; }
    public string? FailureReason { get; init; }
}