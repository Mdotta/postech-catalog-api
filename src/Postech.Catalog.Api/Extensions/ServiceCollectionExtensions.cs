using System.Security.Claims;
using Postech.Catalog.Api.Domain.Authorization;
using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MongoDB.Driver;
using Postech.Catalog.Api.Application.Services;
using Postech.Catalog.Api.Application.Utils;
using Postech.Catalog.Api.Domain.Enums;
using Postech.Catalog.Api.Infrastructure.Cache;
using Postech.Catalog.Api.Infrastructure.Data;
using Postech.Catalog.Api.Infrastructure.DynamoDB;
using Postech.Catalog.Api.Infrastructure.DynamoDB.Repositories;
using Postech.Catalog.Api.Infrastructure.Messaging;
using Postech.Catalog.Api.Infrastructure.MongoDB;
using Postech.Catalog.Api.Infrastructure.MongoDB.Repositories;
using Postech.Catalog.Api.Infrastructure.Repositories;
using Postech.Catalog.Elasticsearch;
using StackExchange.Redis;

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

        // AWS default options (must be before any AddAWSService call)
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
        }

        // Document database (MongoDB or DynamoDB)
        var dynamoSettings = configuration.GetSection("DynamoDB").Get<DynamoDbSettings>();
        if (dynamoSettings is not null && dynamoSettings.UseDynamoDB && !string.IsNullOrWhiteSpace(dynamoSettings.TableName))
        {
            services.AddAWSService<IAmazonDynamoDB>();
            services.AddScoped<IGameDocumentRepository>(sp =>
            {
                var dynamoDb = sp.GetRequiredService<IAmazonDynamoDB>();
                return new GameDynamoRepository(dynamoDb, dynamoSettings.TableName);
            });
        }
        else
        {
            var mongoSettings = configuration.GetSection("MongoDB").Get<MongoDbSettings>();
            if (mongoSettings is not null && !string.IsNullOrWhiteSpace(mongoSettings.ConnectionString))
            {
                var mongoClient = new MongoClient(mongoSettings.ConnectionString);
                var mongoDatabase = mongoClient.GetDatabase(mongoSettings.DatabaseName);
                services.AddSingleton(mongoDatabase);
                services.AddScoped<IGameDocumentRepository, GameMongoRepository>();
            }
        }

        // Redis
        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddScoped<ICacheService, RedisCacheService>();
        }

        // Elasticsearch
        services.AddElasticsearch(configuration);

        // AWS Services (SNS/SQS — options already configured above)
        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
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
            .AddPolicy(Policies.RequireUserRole, policy => policy.RequireAuthenticatedUser());

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