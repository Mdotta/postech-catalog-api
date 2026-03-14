namespace Postech.Catalog.Api.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message,CancellationToken cancellationToken = default) where T : class;
}