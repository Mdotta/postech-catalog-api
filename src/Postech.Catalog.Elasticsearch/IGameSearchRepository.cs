namespace Postech.Catalog.Elasticsearch;

public interface IGameSearchRepository
{
    Task IndexAsync(GameSearchDocument document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameSearchResult>> SearchAsync(string query, int fuzziness, CancellationToken cancellationToken = default);
}
