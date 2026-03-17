using Postech.Catalog.Api.Domain.Enums;

namespace Postech.Catalog.Api.Domain.Entities;

public class Game
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime ReleaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // EF Constructor
    private Game() { }
    
    public Game(string name, string description, decimal price, string genre, DateTime releaseDate)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Price = price;
        Genre = genre;
        ReleaseDate = releaseDate;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void UpdateName(string title)
    {
        Name = title;
    }
    
    public void UpdateDescription(string description)
    {
        Description = description;
    }

    public void UpdatePrice(decimal price)
    {
        Price = price;
    }
    
    public void UpdateGenre(string genre)
    {
        Genre = genre;
    }
    
    public void UpdateReleaseDate(DateTime releaseDate)
    {
        ReleaseDate = releaseDate;
    }
}