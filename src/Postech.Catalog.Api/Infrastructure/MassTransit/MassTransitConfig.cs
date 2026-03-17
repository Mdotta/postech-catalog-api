using System.Reflection;
using MassTransit;
using Postech.Catalog.Api.Infrastructure.Messaging;
using Postech.Shared.Contracts.Events;

namespace Postech.Catalog.Api.Infrastructure.MassTransit;

public static class MassTransitConfig
{
    public static IServiceCollection AddMassTransitServices(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMqPort = configuration.GetValue<ushort>("RabbitMQ:Port", 5672);
        var rabbitMqUser = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitMqPass = configuration["RabbitMQ:Password"] ?? "guest";
        var rabbitMqVHost = configuration["RabbitMQ:VirtualHost"] ?? "/";
        
        services.AddMassTransit(x =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            x.AddConsumers(assembly);
            
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, rabbitMqPort, rabbitMqVHost, h =>
                {
                    h.Username(rabbitMqUser);
                    h.Password(rabbitMqPass);
                    
                    h.RequestedConnectionTimeout(TimeSpan.FromSeconds(30));
                    h.Heartbeat(TimeSpan.FromSeconds(10));
                });
                
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                cfg.PrefetchCount = 16;
                
                cfg.Message<OrderPlacedEvent>(x => 
                    x.SetEntityName("OrderPlacedEvent"));
                
                cfg.Message<OrderProcessedEvent>(x => 
                    x.SetEntityName("OrderProcessedEvent"));
                
                cfg.ConfigureEndpoints(context);
            });
        });
        
        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
        
        return services;
    }
}