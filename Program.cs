using CatalogAPI.Consumers;
using CatalogAPI.Data;
using CatalogAPI.Events;
using CatalogAPI.Interfaces;
using CatalogAPI.Models;
using CatalogAPI.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Porta/URL vinda de ASPNETCORE_URLS (env ou launchSettings); fallback 5050
builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"]);

// Banco PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default") ?? "Host=localhost;Database=catalog;Username=postgres;Password=postgres"));

// Services
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserLibraryService, UserLibraryService>();

// RabbitMQ + MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentProcessedConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Swagger UI (apenas em Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Migra o banco ao iniciar — se Postgres/RabbitMQ não estiverem no ar, a API sobe assim mesmo
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (NpgsqlException ex)
    {
        logger.LogWarning(ex, "PostgreSQL indisponível. Suba com: docker-compose up -d");
    }
}

app.MapOpenApi();

// Health
app.MapGet("/health", () => Results.Ok("OK"));

// ==================== API REQUEST (síncrono) ====================
// CRUD Games — chamadas HTTP diretas, resposta imediata
app.MapGet("/games", async (IGameService svc) => Results.Ok(await svc.GetAllAsync()));
app.MapGet("/games/{id:guid}", async (Guid id, IGameService svc) =>
{
    var g = await svc.GetByIdAsync(id);
    return g is null ? Results.NotFound() : Results.Ok(g);
});
app.MapPost("/games", async (CreateGameRequest req, IGameService svc) =>
{
    if (req.Price <= 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["Price"] = ["Price must be greater than 0."] });
    var game = await svc.CreateAsync(req.Title, req.Description, req.Developer, req.Publisher, req.Price);
    return Results.Created($"/games/{game.Id}", game);
});
app.MapPut("/games/{id:guid}", async (Guid id, UpdateGameRequest req, IGameService svc) =>
{
    if (req.Price is decimal p && p <= 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["Price"] = ["Price must be greater than 0."] });
    var g = await svc.UpdateAsync(id, req.Title, req.Description, req.Developer, req.Publisher, req.Price);
    return g is null ? Results.NotFound() : Results.Ok(g);
});
app.MapDelete("/games/{id:guid}", async (Guid id, IGameService svc) =>
{
    var ok = await svc.DeleteAsync(id);
    return ok ? Results.NoContent() : Results.NotFound();
});

// Place Order — API recebe request, mas publica na FILA (resposta 202 Accepted)
// O pagamento é processado assincronamente pelo PaymentService via RabbitMQ
app.MapPost("/orders", async (PlaceOrderRequest req, IOrderService svc) =>
{
    var (orderId, success, error) = await svc.PlaceOrderAsync(req.UserId, req.GameId);
    if (!success) return Results.NotFound(error);
    return Results.Accepted($"/orders/{orderId}", new { orderId, status = "Pending" });
});

// Biblioteca do usuário — API REQUEST (leitura síncrona)
app.MapGet("/users/{userId:guid}/library", async (Guid userId, IUserLibraryService svc) =>
{
    var items = await svc.GetLibraryAsync(userId);
    return Results.Ok(items);
});

app.Run();
// DTOs inline (minimal API binding)
record CreateGameRequest(string Title, string Description, string Developer, string Publisher, decimal Price);
record UpdateGameRequest(string? Title, string? Description, string? Developer, string? Publisher, decimal? Price);
record PlaceOrderRequest(Guid UserId, Guid GameId);

