namespace Postech.Catalog.Elasticsearch;

public class GameSearchDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime ReleaseDate { get; set; }
    public List<string> Tags { get; set; } = [];
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
}
