using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Postech.Catalog.Elasticsearch;

public class GameSearchRepository : IGameSearchRepository
{
    private const string IndexName = "games";

    private readonly ElasticsearchClient _client;

    public GameSearchRepository(ElasticsearchClient client)
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
                .MultiMatch(mm => mm
                    .Query(query)
                    .Fields(new[] { "name^3", "description", "genre^2", "tags", "developer", "publisher" })
                    .Type(TextQueryType.BestFields)
                    .Fuzziness(new Fuzziness(fuzziness))
                )
            ), cancellationToken);

        return response.Hits
            .Select(hit => new GameSearchResult
            {
                Document = hit.Source!,
                Score = hit.Score ?? 0
            })
            .ToList();
    }
}
