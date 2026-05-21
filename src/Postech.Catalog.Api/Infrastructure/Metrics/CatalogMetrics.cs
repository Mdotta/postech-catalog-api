using Prometheus;

namespace Postech.Catalog.Api.Infrastructure.Metrics;

public static class CatalogMetrics
{
    public static readonly Counter GamesCreated = Prometheus.Metrics.CreateCounter(
        "games_created_total", "Total number of games created");

    public static readonly Counter GamesUpdated = Prometheus.Metrics.CreateCounter(
        "games_updated_total", "Total number of games updated");

    public static readonly Counter GamesDeleted = Prometheus.Metrics.CreateCounter(
        "games_deleted_total", "Total number of games deleted");

    public static readonly Counter OrdersCreated = Prometheus.Metrics.CreateCounter(
        "orders_created_total", "Total number of orders created");

    public static readonly Counter CacheHits = Prometheus.Metrics.CreateCounter(
        "cache_hit_total", "Total Redis cache hits");

    public static readonly Counter CacheMisses = Prometheus.Metrics.CreateCounter(
        "cache_miss_total", "Total Redis cache misses");

    public static readonly Counter DocumentSync = Prometheus.Metrics.CreateCounter(
        "document_sync_total", "Document store sync results",
        new CounterConfiguration { LabelNames = ["status"] });

    public static readonly Counter PaymentProcessed = Prometheus.Metrics.CreateCounter(
        "payment_processed_total", "Payment events processed by SQS consumer",
        new CounterConfiguration { LabelNames = ["status"] });
}
