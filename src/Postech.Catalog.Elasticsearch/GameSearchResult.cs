namespace Postech.Catalog.Elasticsearch;

public class GameSearchResult
{
    public GameSearchDocument Document { get; init; } = null!;
    public double Score { get; init; }
}
