using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Postech.Catalog.Api.Application.Services;
using Postech.Catalog.Api.Domain.Authorization;

namespace Postech.Catalog.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/game");
        
        group.MapGet("/", () => ListGamesAsync())
            .WithName("GetGames")
            .WithSummary("Get all games");
        
        group.MapPost("", () => CreateGameAsync())
            .WithName("CreateGame")
            .WithSummary("Create a new game")
            .RequireAuthorization(Policies.RequireAdminRole)
            .Produces(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        
        group.MapPatch("/{id}", (Guid id) => UpdateGameAsync(id))
            .WithName("UpdateGame")
            .WithSummary("Update an existing game")
            .RequireAuthorization(Policies.RequireAdminRole)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        
        group.MapDelete("/{id}", (Guid id) => DeleteGameAsync(id))
            .WithName("DeleteGame")
            .WithSummary("Delete an existing game")
            .RequireAuthorization(Policies.RequireAdminRole)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        
        group.MapPost("/create-order", () => Results.Ok("Order created"))
            .WithName("CreateOrder")
            .WithDescription("Creates a new order for the user.")
            .RequireAuthorization(Policies.RequireUserRole);
        
        group.MapGet("/library", () => Results.Ok("User's game library"))
            .WithName("GetLibrary")
            .WithDescription("Retrieves the authenticated user's game library.")
            .RequireAuthorization(Policies.RequireUserRole);
    }

    private static Task DeleteGameAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    private static Task UpdateGameAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    private static object CreateGameAsync()
    {
        //TODO: Implement actual game creation logic here
        return new { Id = Guid.NewGuid(), Name = "New Game", Genre = "Strategy" };
    }

    private static object ListGamesAsync()
    {
        //TODO: Implement actual data retrieval logic here
        return new[]
        {
            new { Id = Guid.NewGuid(), Name = "Game 1", Genre = "Action" },
            new { Id = Guid.NewGuid(), Name = "Game 2", Genre = "Adventure" },
            new { Id = Guid.NewGuid(), Name = "Game 3", Genre = "RPG" }
        };
    }
    //
    // private static async Task<IResult> UpdateUserStatus(
    //     [FromRoute] Guid id,
    //     [FromBody] RequestUpdateUserRole request,
    //     [FromServices] IUserService userService,
    //     CancellationToken cancellationToken)
    // {
    //     var result = await userService.UpdateRole(id, request, cancellationToken);
    //
    //     if (result.IsError)
    //     {
    //         return Results.NotFound(new ProblemDetails
    //         {
    //             Status = StatusCodes.Status404NotFound,
    //             Title = "User not found",
    //             Detail = string.Join(";\n", result.Errors.Select(e=>e.Description).ToArray())
    //         });
    //     }
    //     
    //     return Results.NoContent();
    // }
    //
    // private static async Task<IResult> GetCurrentUserAsync(
    //     ClaimsPrincipal user,
    //     [FromServices] IUserService userService,
    //     CancellationToken cancellationToken)
    // {
    //     var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //
    //     if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    //     {
    //         return Results.Unauthorized();
    //     }
    //
    //     var result = await userService.GetUserByIdAsync(userId, cancellationToken);
    //
    //     if (result.IsError)
    //     {
    //         return Results.NotFound(new ProblemDetails
    //         {
    //             Status = StatusCodes.Status404NotFound,
    //             Title = "User not found",
    //             Detail = string.Join(";\n", result.Errors.Select(e => e.Description))
    //         });
    //     }
    //
    //     return Results.Ok(result.Value);
    // }
}