namespace CatalogAPI.Models;

public class UserLibraryItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public Guid OrderId { get; set; }
    public DateTime AddedAt { get; set; }
}
