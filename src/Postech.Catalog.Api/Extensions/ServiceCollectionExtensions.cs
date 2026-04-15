using System.Security.Claims;
using Postech.Catalog.Api.Domain.Authorization;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Postech.Catalog.Api.Application.Services;
using Postech.Catalog.Api.Application.Utils;
using Postech.Catalog.Api.Domain.Enums;
using Postech.Catalog.Api.Infrastructure.Data;
using Postech.Catalog.Api.Infrastructure.Messaging;
using Postech.Catalog.Api.Infrastructure.Repositories;

namespace Postech.Catalog.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        
        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                                 ?? throw new InvalidOperationException("Database connection string is not configured");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IGameRepository, GameRepository>();

        // AWS Services
        var serviceUrl = configuration["AWS:ServiceURL"];

        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
                new AmazonSimpleNotificationServiceClient(
                    new AmazonSimpleNotificationServiceConfig { ServiceURL = serviceUrl }));
            services.AddSingleton<IAmazonSQS>(_ =>
                new AmazonSQSClient(
                    new AmazonSQSConfig { ServiceURL = serviceUrl }));
        }
        else
        {
            services.AddDefaultAWSOptions(configuration.GetAWSOptions());
            services.AddAWSService<IAmazonSimpleNotificationService>();
            services.AddAWSService<IAmazonSQS>();
        }

        // Messaging - SNS for publishing events
        services.AddScoped<IEventPublisher, SnsEventPublisher>();
        
        // SQS Consumer for order events
        services.AddHostedService<SqsOrderEventConsumer>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Note: JWT validation is now handled by API Gateway (Cognito)
        // This service only extracts claims from the token for policy checks
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "PassThrough";
            options.DefaultChallengeScheme = "PassThrough";
        })
        .AddScheme<PassThroughAuthenticationOptions, PassThroughAuthenticationHandler>("PassThrough", null);
    
        // Configure Policies - these will check claims set by API Gateway
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.RequireAdminRole, policy => policy.RequireRole(UserRoles.Administrator.ToString()))
            .AddPolicy(Policies.RequireUserRole, policy => policy.RequireRole(UserRoles.User.ToString(), UserRoles.Administrator.ToString()));

        return services;
    }

    public static IServiceCollection AddOpenApiWithAuth(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                // Define the Bearer security scheme
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme."
                };

                // Apply global security requirement using the new syntax
                document.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                    }
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }
}