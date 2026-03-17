using MassTransit;
using Postech.Catalog.Api.Domain.Enums;
using Postech.Catalog.Api.Infrastructure.Repositories;
using Postech.Shared.Contracts.Events;


namespace Postech.Catalog.Api.Application.Consumers;

public class OrderProcessedConsumer(ILogger<OrderProcessedConsumer> logger, IOrderRepository orderRepository):IConsumer<OrderProcessedEvent>
{
    public async Task Consume(ConsumeContext<OrderProcessedEvent> context)
    {
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", context.CorrelationId))
        {
            logger.LogInformation($"Order with id {context.Message.OrderId} begin processing.");
        
            var order = await orderRepository.GetByIdAsync(context.Message.OrderId);
            if (order == null)
            {
                logger.LogError($"Order with id {context.Message.OrderId} does not exist");
                return;
            }

            if (order.Status != OrderStatus.Placed)
            {
                logger.LogError($"Order with id {context.Message.OrderId} was already processed. Current status: {order.Status}");
                return;
            }

            if (context.Message.IsSuccessful)
            {
                order.Status = OrderStatus.Completed;
                logger.LogInformation($"Order with id {context.Message.OrderId} processed successfully");
            }
            else
            {
                order.Status = OrderStatus.Cancelled;
                logger.LogInformation($"Order with id {context.Message.OrderId} failed to process and has been cancelled.\nReason: {context.Message.FailureReason}");
            }
        }
    }
}