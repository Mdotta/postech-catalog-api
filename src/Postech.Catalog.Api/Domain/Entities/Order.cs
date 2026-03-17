using Postech.Catalog.Api.Domain.Enums;

namespace Postech.Catalog.Api.Domain.Entities;

public class Order
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime PlacedAt { get; set; }
    public OrderStatus Status { get; set; }
}