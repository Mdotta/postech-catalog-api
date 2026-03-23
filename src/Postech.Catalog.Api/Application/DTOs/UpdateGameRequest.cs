namespace Postech.Catalog.Api.Application.DTOs;

public record UpdateGameRequest(
    Guid Id,
    string? Name = default,
    string? Description = default,
    decimal? Price = default,
    string? Genre = default,
    DateOnly? ReleaseDate = default
);