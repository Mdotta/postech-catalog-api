using Postech.Catalog.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Postech.Catalog.Api.Infrastructure.Data;
using Postech.Catalog.Api.Middleware;
using Scalar.AspNetCore;

namespace Postech.Catalog.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Middleware
        app.UseMiddleware<CorrelationIdMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        // Scalar
        app.MapOpenApi();
        app.MapScalarApiReference();

        // Map Endpoints
        app.MapCatalogEndpoints();
        app.MapHealthEndpoints();

        return app;
    }

    public static async Task<WebApplication> ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        
        await dbContext.Database.MigrateAsync();
        
        return app;
    }
}