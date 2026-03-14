namespace Postech.Catalog.Api.Application.Utils;

public interface ICorrelationContext
{
    Guid CorrelationId { get; set; }
}

