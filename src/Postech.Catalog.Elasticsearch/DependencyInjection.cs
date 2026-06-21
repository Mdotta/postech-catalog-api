using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Postech.Catalog.Elasticsearch;

public static class DependencyInjection
{
    public static IServiceCollection AddElasticsearch(
        this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["Elasticsearch:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            return services;

        var settings = new ElasticsearchClientSettings(new Uri(endpoint))
            .DefaultIndex("games")
            .RequestTimeout(TimeSpan.FromSeconds(3));

        services.AddSingleton(new ElasticsearchClient(settings));
        services.AddScoped<IGameSearchRepository, GameSearchRepository>();

        return services;
    }
}
