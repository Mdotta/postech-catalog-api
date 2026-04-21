namespace Postech.Catalog.Api.Application.DTOs;

public record CreateGameRequest(
    string Name,
    string Description,
    decimal Price,
    string Genre,
    DateTime ReleaseDate,
    // Campos expandidos — salvos apenas no MongoDB
    List<string>? Tags = null,
    List<string>? Screenshots = null,
    string? Developer = null,
    string? Publisher = null
);