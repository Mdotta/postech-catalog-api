using ErrorOr;

namespace Postech.Catalog.Api.Domain.Errors;

public static partial class Errors
{
    public static class Game
    {
        public static Error NameRequired => Error.Validation(
            code: "Game.Name.Required",
            description: "Name is required.");

        public static Error NotFound => Error.NotFound(
            code: "Game.NotFound",
            description: "Game not found.");
    }
}

