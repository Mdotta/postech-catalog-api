using Postech.Catalog.Api.Application.Events;
using Postech.Catalog.Api.Infrastructure.Messaging;

namespace Postech.Catalog.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithName("HealthCheck")
            .WithTags("Health")
            .AllowAnonymous();

        app.MapPost("/health/rabbitmq", async (IEventPublisher eventPublisher, ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("Testing RabbitMQ connection...");

                var testEvent = new OrderPlacedEvent()
                {
                    OrderId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    TotalAmount = 0.00m,
                    PlacedAt = DateTime.UtcNow
                };

                await eventPublisher.PublishAsync(testEvent);
                
                logger.LogInformation("RabbitMQ test message sent successfully");
                
                return Results.Ok(new 
                { 
                    status = "success", 
                    message = "RabbitMQ connection is working",
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish test message to RabbitMQ");
                
                return Results.Problem(
                    title: "RabbitMQ Connection Failed",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("TestRabbitMQConnection")
        .WithTags("Health")
        .AllowAnonymous();

        return app;
    }
}

