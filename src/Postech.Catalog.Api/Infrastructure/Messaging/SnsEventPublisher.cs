using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Postech.Catalog.Api.Application.Utils;

namespace Postech.Catalog.Api.Infrastructure.Messaging;

public class SnsEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly ILogger<SnsEventPublisher> _logger;
    private readonly string _topicArn;
    private readonly ICorrelationContext _correlationContext;

    public SnsEventPublisher(IAmazonSimpleNotificationService snsClient,
        ILogger<SnsEventPublisher> logger, 
        IConfiguration configuration,
        ICorrelationContext correlationContext)
    {
        _snsClient = snsClient;
        _logger = logger;
        _correlationContext = correlationContext;
        _topicArn = configuration["AWS:SnsTopicArn"]
                    ?? throw new InvalidOperationException("AWS SNS Topic ARN not configured");
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogInformation("Publishing event {EventType} to SNS with CorrelationId {CorrelationId}", 
                typeof(T).Name, _correlationContext.CorrelationId);
            
            var request = new PublishRequest
            {
                TopicArn = _topicArn,
                Message = JsonSerializer.Serialize(message),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["EventType"] = new() { DataType = "String", StringValue = typeof(T).Name },
                    ["CorrelationId"] = new() { DataType = "String", StringValue = _correlationContext.CorrelationId.ToString() }
                }
            };
            
            await _snsClient.PublishAsync(request, cancellationToken);
            _logger.LogInformation("Event {EventType} successfully published to SNS", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to SNS", typeof(T).Name);
            throw;
        }
    }
}
