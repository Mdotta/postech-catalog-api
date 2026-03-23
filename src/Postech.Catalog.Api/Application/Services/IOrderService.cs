using ErrorOr;

namespace Postech.Catalog.Api.Application.Services;

public interface IOrderService
{
    Task<ErrorOr<Guid>> PlaceOrder(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
}
