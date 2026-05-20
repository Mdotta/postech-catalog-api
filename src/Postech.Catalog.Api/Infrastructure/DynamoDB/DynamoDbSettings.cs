namespace Postech.Catalog.Api.Infrastructure.DynamoDB;

public class DynamoDbSettings
{
    public bool UseDynamoDB { get; set; }
    public string TableName { get; set; } = "postech_catalog_games";
}
