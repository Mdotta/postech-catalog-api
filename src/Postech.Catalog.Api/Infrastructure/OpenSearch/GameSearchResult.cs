namespace Postech.Catalog.Api.Infrastructure.OpenSearch;

public class GameSearchResult
{
    public GameSearchDocument Document { get; init; } = null!;
    public double Score { get; init; }
}
