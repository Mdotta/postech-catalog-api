using OpenSearch.Client;

namespace Postech.Catalog.Api.Infrastructure.OpenSearch;

public class GameSearchRepository : IGameSearchRepository
{
    private const string IndexName = "games";

    private readonly IOpenSearchClient _client;

    public GameSearchRepository(IOpenSearchClient client)
    {
        _client = client;
    }

    public async Task IndexAsync(GameSearchDocument document, CancellationToken cancellationToken = default)
    {
        await _client.IndexAsync(document, idx => idx.Index(IndexName).Id(document.Id), cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync<GameSearchDocument>(id, d => d.Index(IndexName), cancellationToken);
    }

    public async Task<IReadOnlyList<GameSearchResult>> SearchAsync(
        string query, int fuzziness, CancellationToken cancellationToken = default)
    {
        var response = await _client.SearchAsync<GameSearchDocument>(s => s
            .Index(IndexName)
            .Query(q => q
                .MultiMatch(m => m
                    .Fields(f => f
                        .Field(ff => ff.Name, 3.0)
                        .Field(ff => ff.Description)
                        .Field(ff => ff.Genre, 2.0)
                        .Field(ff => ff.Tags)
                        .Field(ff => ff.Developer)
                        .Field(ff => ff.Publisher))
                    .Query(query)
                    .Fuzziness(Fuzziness.EditDistance(fuzziness))
                    .Type(TextQueryType.BestFields)
                )
            ), cancellationToken);

        return response.Hits
            .Select(hit => new GameSearchResult
            {
                Document = hit.Source,
                Score = hit.Score ?? 0
            })
            .ToList();
    }
}
