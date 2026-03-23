using System.Security.Claims;
using ErrorOr;
using Postech.Catalog.Api.Application.DTOs;
using Postech.Catalog.Api.Application.Services;
using Postech.Catalog.Api.Domain.Authorization;

namespace Postech.Catalog.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/game");

        group.MapGet("/", ListGamesAsync)
            .WithName("GetGames")
            .WithSummary("Get all games");

        group.MapGet("/{id:guid}", GetGameByIdAsync)
            .WithName("GetGameById")
            .WithSummary("Get game by id");

        group.MapPost("/", CreateGameAsync)
            .WithName("CreateGame")
            .WithSummary("Create a new game")
            .RequireAuthorization(Policies.RequireAdminRole)
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPatch("/{id:guid}", UpdateGameAsync)
            .WithName("UpdateGame")
            .WithSummary("Update an existing game")
            .RequireAuthorization(Policies.RequireAdminRole)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteGameAsync)
            .WithName("DeleteGame")
            .WithSummary("Delete an existing game")
            .RequireAuthorization(Policies.RequireAdminRole)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/create-order", PlaceOrderAsync)
            .WithName("CreateOrder")
            .WithSummary("Creates a new order for the authenticated user.")
            .RequireAuthorization(Policies.RequireUserRole)
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/library", () => Results.Ok("User's game library"))
            .WithName("GetLibrary")
            .WithDescription("Retrieves the authenticated user's game library.")
            .RequireAuthorization(Policies.RequireUserRole);
    }

    private static async Task<IResult> ListGamesAsync(IGameService gameService, CancellationToken cancellationToken)
    {
        var result = await gameService.GetAllGamesAsync(cancellationToken);
        return ToHttpResult(result);
    }

    private static async Task<IResult> GetGameByIdAsync(Guid id, IGameService gameService, CancellationToken cancellationToken)
    {
        var result = await gameService.GetGameByIdAsync(id, cancellationToken);
        return ToHttpResult(result);
    }

    private static async Task<IResult> CreateGameAsync(CreateGameRequest request, IGameService gameService, CancellationToken cancellationToken)
    {
        var result = await gameService.CreateGameAsync(request, cancellationToken);
        if (result.IsError)
            return ToHttpResult(result);

        return Results.Created();
    }

    private static async Task<IResult> UpdateGameAsync(
        Guid id,
        UpdateGameRequest body,
        IGameService gameService,
        CancellationToken cancellationToken)
    {
        var request = body with { Id = id };
        var result = await gameService.UpdateGameAsync(request, cancellationToken);
        if (result.IsError)
            return ToHttpResult(result);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteGameAsync(Guid id, IGameService gameService, CancellationToken cancellationToken)
    {
        var result = await gameService.DeleteGameAsync(id, cancellationToken);
        if (result.IsError)
            return ToHttpResult(result);

        return Results.NoContent();
    }

    private static async Task<IResult> PlaceOrderAsync(
        ClaimsPrincipal user,
        PlaceOrderRequest body,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        if (body.GameId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "GameId is required." });
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var result = await orderService.PlaceOrder(userId, body.GameId, cancellationToken);
        if (result.IsError)
        {
            var err = result.FirstError;
            return err.Type == ErrorType.NotFound
                ? Results.NotFound()
                : Results.Problem(title: "Could not place order", detail: err.Description, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Created($"/api/game/orders/{result.Value}", new { orderId = result.Value });
    }

    private static IResult ToHttpResult<T>(ErrorOr<T> result)
    {
        if (!result.IsError)
            return Results.Ok(result.Value);

        var err = result.FirstError;
        return err.Type switch
        {
            ErrorType.NotFound => Results.NotFound(),
            ErrorType.Validation => Results.BadRequest(new { error = err.Description, code = err.Code }),
            _ => Results.Problem(title: "Request failed", detail: err.Description, statusCode: StatusCodes.Status400BadRequest)
        };
    }
}
