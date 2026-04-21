using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Postech.Catalog.Api.Infrastructure.MongoDB.Documents;

public class GameDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime ReleaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Campos expandidos — exclusivos do MongoDB (sem migration no Postgres)
    public List<string> Tags { get; set; } = [];
    public List<string> Screenshots { get; set; } = [];
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
}
