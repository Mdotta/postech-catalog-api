using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Postech.Catalog.Api.Infrastructure.MongoDB.Documents;
using Postech.Catalog.Api.Infrastructure.Repositories;

namespace Postech.Catalog.Api.Infrastructure.DynamoDB.Repositories;

public class GameDynamoRepository : IGameDocumentRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public GameDynamoRepository(IAmazonDynamoDB dynamoDb, string tableName)
    {
        _dynamoDb = dynamoDb;
        _tableName = tableName;
    }

    public async Task<GameDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["Id"] = new AttributeValue { S = id.ToString() }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request, cancellationToken);
        if (response.Item is null || response.Item.Count == 0)
            return null;

        return MapToDocument(response.Item);
    }

    public async Task UpsertAsync(GameDocument document, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = MapToItem(document)
        };

        await _dynamoDb.PutItemAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["Id"] = new AttributeValue { S = id.ToString() }
            }
        };

        await _dynamoDb.DeleteItemAsync(request, cancellationToken);
    }

    private static Dictionary<string, AttributeValue> MapToItem(GameDocument doc)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["Id"] = new AttributeValue { S = doc.Id.ToString() },
            ["Name"] = new AttributeValue { S = doc.Name },
            ["Description"] = new AttributeValue { S = doc.Description },
            ["Genre"] = new AttributeValue { S = doc.Genre },
            ["Price"] = new AttributeValue { N = doc.Price.ToString("F2") },
            ["ReleaseDate"] = new AttributeValue { S = doc.ReleaseDate.ToString("O") },
            ["CreatedAt"] = new AttributeValue { S = doc.CreatedAt.ToString("O") },
            ["UpdatedAt"] = new AttributeValue { S = doc.UpdatedAt.ToString("O") },
            ["Developer"] = new AttributeValue { S = doc.Developer },
            ["Publisher"] = new AttributeValue { S = doc.Publisher }
        };

        if (doc.Tags.Count > 0)
            item["Tags"] = new AttributeValue { SS = doc.Tags };

        if (doc.Screenshots.Count > 0)
            item["Screenshots"] = new AttributeValue { SS = doc.Screenshots };

        return item;
    }

    private static GameDocument MapToDocument(Dictionary<string, AttributeValue> item)
    {
        return new GameDocument
        {
            Id = Guid.Parse(item["Id"].S),
            Name = item.TryGetValue("Name", out var name) ? name.S : string.Empty,
            Description = item.TryGetValue("Description", out var desc) ? desc.S : string.Empty,
            Genre = item.TryGetValue("Genre", out var genre) ? genre.S : string.Empty,
            Price = item.TryGetValue("Price", out var price) ? decimal.Parse(price.N) : 0m,
            ReleaseDate = item.TryGetValue("ReleaseDate", out var rd) ? DateTime.Parse(rd.S) : DateTime.MinValue,
            CreatedAt = item.TryGetValue("CreatedAt", out var ca) ? DateTime.Parse(ca.S) : DateTime.MinValue,
            UpdatedAt = item.TryGetValue("UpdatedAt", out var ua) ? DateTime.Parse(ua.S) : DateTime.MinValue,
            Developer = item.TryGetValue("Developer", out var dev) ? dev.S : string.Empty,
            Publisher = item.TryGetValue("Publisher", out var pub) ? pub.S : string.Empty,
            Tags = item.TryGetValue("Tags", out var tags) ? tags.SS : [],
            Screenshots = item.TryGetValue("Screenshots", out var ss) ? ss.SS : []
        };
    }
}
