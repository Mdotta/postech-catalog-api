namespace Postech.Catalog.Api.Application.Utils;

public class CorrelationContext : ICorrelationContext
{
    public Guid CorrelationId { get; set; }
}