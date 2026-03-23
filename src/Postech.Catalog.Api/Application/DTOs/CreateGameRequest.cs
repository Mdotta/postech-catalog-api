namespace Postech.Catalog.Api.Application.DTOs;

public record CreateGameRequest(
    string Name,
    string Description,
    decimal Price,
    string Genre,
    DateOnly ReleaseDate
);