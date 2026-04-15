using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Postech.Catalog.Api.Infrastructure.Repositories;
using Postech.Catalog.Api.Domain.Enums;
using Postech.Shared.Contracts.Events;

namespace Postech.Catalog.Api.Infrastructure.Messaging;

public class SqsOrderEventConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<SqsOrderEventConsumer> _logger;
    private readonly string _queueUrl;
    private readonly IServiceProvider _serviceProvider;

    public SqsOrderEventConsumer(
        IAmazonSQS sqsClient,
        ILogger<SqsOrderEventConsumer> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _queueUrl = configuration["AWS:SqsQueueUrl"]
                    ?? throw new InvalidOperationException("AWS SQS Queue URL not configured");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQS Order Event Consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20,
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (response.Messages.Count == 0)
                {
                    continue;
                }

                foreach (var message in response.Messages)
                {
                    try
                    {
                        await ProcessMessageAsync(message, stoppingToken);
                        
                        // Delete message after successful processing
                        await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing SQS message {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from SQS");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("SQS Order Event Consumer stopped");
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            var eventType = message.MessageAttributes.ContainsKey("EventType")
                ? message.MessageAttributes["EventType"].StringValue
                : null;

            _logger.LogInformation("Processing SQS message {MessageId} of type {EventType}", 
                message.MessageId, eventType);

            // Parse the SNS message wrapper (SQS receives SNS messages as JSON)
            var snsMessage = JsonSerializer.Deserialize<SnsMessageWrapper>(message.Body);
            if (snsMessage == null)
            {
                _logger.LogWarning("Failed to deserialize SNS message from SQS");
                return;
            }

            // Extract correlation ID if available
            if (snsMessage.MessageAttributes?.ContainsKey("CorrelationId") == true)
            {
                var correlationId = snsMessage.MessageAttributes["CorrelationId"].Value;
                // You might want to set this in a context for logging
            }

            // Handle OrderProcessedEvent
            if (eventType == nameof(OrderProcessedEvent))
            {
                var orderEvent = JsonSerializer.Deserialize<OrderProcessedEvent>(snsMessage.Message);
                if (orderEvent != null)
                {
                    await HandleOrderProcessedEventAsync(orderEvent, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message content");
            throw;
        }
    }

    private async Task HandleOrderProcessedEventAsync(OrderProcessedEvent orderEvent, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        _logger.LogInformation("Handling OrderProcessedEvent for Order {OrderId}", orderEvent.OrderId);

        var order = await orderRepository.GetByIdAsync(orderEvent.OrderId);
        if (order == null)
        {
            _logger.LogError("Order with id {OrderId} does not exist", orderEvent.OrderId);
            return;
        }

        if (order.Status != OrderStatus.Placed)
        {
            _logger.LogWarning("Order with id {OrderId} was already processed. Current status: {Status}", 
                orderEvent.OrderId, order.Status);
            return;
        }

        if (orderEvent.IsSuccessful)
        {
            order.Status = OrderStatus.Completed;
            _logger.LogInformation("Order with id {OrderId} processed successfully", orderEvent.OrderId);
        }
        else
        {
            order.Status = OrderStatus.Cancelled;
            _logger.LogInformation("Order with id {OrderId} failed to process. Reason: {Reason}", 
                orderEvent.OrderId, orderEvent.FailureReason);
        }

        await orderRepository.UpdateAsync(order, cancellationToken);
    }
}

// Helper class to deserialize SNS message wrapper from SQS
public class SnsMessageWrapper
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, SnsMessageAttribute>? MessageAttributes { get; set; }
}

public class SnsMessageAttribute
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
