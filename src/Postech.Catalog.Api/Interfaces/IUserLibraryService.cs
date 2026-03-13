namespace CatalogAPI.Interfaces;

public interface IUserLibraryService
{
    /// <summary>
    /// Lista jogos da biblioteca do usuário.
    /// Usado pela API (GET /users/{id}/library).
    /// </summary>
    Task<IReadOnlyList<UserLibraryItemDto>> GetLibraryAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Adiciona jogo à biblioteca após pagamento aprovado.
    /// Usado pelo Consumer (FILA) — PaymentProcessedConsumer chama este método.
    /// NÃO é chamado via API HTTP.
    /// </summary>
    Task<bool> AddToLibraryAsync(Guid orderId, Guid userId, Guid gameId, CancellationToken ct = default);
}

public record UserLibraryItemDto(DateTime AddedAt, Guid Id, string Title);
