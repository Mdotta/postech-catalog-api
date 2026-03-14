using Postech.Catalog.Api.Domain.Entities;

namespace Postech.Catalog.Api.Application.DTOs;

public record GameResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Genre,
    DateTime ReleaseDate
);