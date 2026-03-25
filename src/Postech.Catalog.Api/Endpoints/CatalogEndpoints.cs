using System.Security.Claims;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Postech.Catalog.Api.Application.DTOs;
using Postech.Catalog.Api.Application.Services;
using Postech.Catalog.Api.Application.Validations;
using Postech.Catalog.Api.Domain.Authorization;

namespace Postech.Catalog.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/game");

        group.MapGet("/", async ([FromServices] IGameService gameService, CancellationToken ct) =>
                await ListGamesAsync(gameService, ct))
            .WithName("GetGames")
            .WithSummary("Get all games")
            .Produces<List<GameResponse>>(StatusCodes.Status200OK);

        group.MapPost("", async ([FromBody] CreateGameRequest request, [FromServices] IGameService gameService, CancellationToken ct) =>
                await CreateGameAsync(request, gameService, ct))
            .WithName("CreateGame")
            .WithSummary("Create a new game")
            .RequireAuthorization(Policies.RequireAdminRole)
            .Produces(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPatch("/{id}", async (Guid id, [FromBody] UpdateGameRequest request, [FromServices] IGameService gameService, CancellationToken ct) =>
                await UpdateGameAsync(id, request, gameService, ct))
            .WithName("UpdateGame")
            .WithSummary("Update an existing game")
            .RequireAuthorization(Policies.RequireAdminRole)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", async (Guid id, [FromServices] IGameService gameService, CancellationToken ct) =>
                await DeleteGameAsync(id, gameService, ct))
            .WithName("DeleteGame")
            .WithSummary("Delete an existing game")
            .RequireAuthorization(Policies.RequireAdminRole)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/create-order", async (ClaimsPrincipal user, [FromBody] CreateOrderRequest request, [FromServices] IOrderService orderService) =>
                await CreateOrderAsync(user, request, orderService))
            .WithName("CreateOrder")
            .WithSummary("Place a new order for a game")
            .WithDescription("Publishes an OrderPlacedEvent and returns 202 Accepted immediately. Order completion is handled asynchronously via RabbitMQ.")
            .RequireAuthorization(Policies.RequireUserRole)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/library", async (ClaimsPrincipal user, [FromServices] IOrderService orderService, CancellationToken ct) =>
                await GetLibraryAsync(user, orderService, ct))
            .WithName("GetLibrary")
            .WithSummary("Get the authenticated user's game library")
            .WithDescription("Returns games from orders with status Completed.")
            .RequireAuthorization(Policies.RequireUserRole)
            .Produces<List<GameResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> ListGamesAsync(IGameService gameService, CancellationToken ct)
    {
        var result = await gameService.GetAllGamesAsync(ct);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> CreateGameAsync(CreateGameRequest request, IGameService gameService, CancellationToken ct)
    {
        var validation = RegisterGameRequestValidator.Validate(request);
        if (validation.IsError)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = string.Join(";\n", validation.Errors.Select(e => e.Description))
            });
        }

        var result = await gameService.CreateGameAsync(request, ct);
        if (result.IsError)
            return ToErrorResult(result.Errors);

        return Results.StatusCode(StatusCodes.Status201Created);
    }

    private static async Task<IResult> UpdateGameAsync(Guid id, UpdateGameRequest request, IGameService gameService, CancellationToken ct)
    {
        var requestWithId = request with { Id = id };
        var result = await gameService.UpdateGameAsync(requestWithId, ct);

        if (result.IsError)
            return ToErrorResult(result.Errors);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteGameAsync(Guid id, IGameService gameService, CancellationToken ct)
    {
        var result = await gameService.DeleteGameAsync(id, ct);

        if (result.IsError)
            return ToErrorResult(result.Errors);

        return Results.NoContent();
    }

    private static async Task<IResult> CreateOrderAsync(ClaimsPrincipal user, CreateOrderRequest request, IOrderService orderService)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var result = await orderService.PlaceOrder(userId, request.GameId);
        if (result.IsError)
            return ToErrorResult(result.Errors);

        return Results.Accepted(uri: null, value: new { orderId = result.Value });
    }

    private static async Task<IResult> GetLibraryAsync(ClaimsPrincipal user, IOrderService orderService, CancellationToken ct)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var result = await orderService.GetUserLibraryAsync(userId, ct);
        return Results.Ok(result.Value);
    }

    private static IResult ToErrorResult(List<Error> errors)
    {
        var first = errors[0];
        return first.Type switch
        {
            ErrorType.NotFound => Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = string.Join(";\n", errors.Select(e => e.Description))
            }),
            ErrorType.Validation => Results.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = string.Join(";\n", errors.Select(e => e.Description))
            }),
            _ => Results.Problem(
                detail: string.Join(";\n", errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
