using ErrorOr;
using Postech.Catalog.Api.Application.DTOs;
using Postech.Catalog.Api.Domain.Errors;

namespace Postech.Catalog.Api.Application.Validations;

public static class RegisterGameRequestValidator
{
    public static ErrorOr<Success> Validate(CreateGameRequest request)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(Errors.Game.NameRequired);

        if (request.Price <= 0)
            errors.Add(Error.Validation("Game.Price.Invalid", "Price must be greater than zero."));

        if (string.IsNullOrWhiteSpace(request.Genre))
            errors.Add(Error.Validation("Game.Genre.Required", "Genre is required."));

        if (request.ReleaseDate == default)
            errors.Add(Error.Validation("Game.ReleaseDate.Invalid", "ReleaseDate is required."));

        if (errors.Any())
            return errors;

        return Result.Success;
    }
}