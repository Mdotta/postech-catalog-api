namespace Postech.Catalog.Api.Application.DTOs;

public record UpdateGameRequest(
    Guid Id,
    string? Name = default,
    string? Description = default,
    decimal? Price = default,
    string? Genre = default,
    DateTime? ReleaseDate = default,
    // Campos expandidos — salvos apenas no MongoDB
    List<string>? Tags = null,
    List<string>? Screenshots = null,
    string? Developer = null,
    string? Publisher = null
);