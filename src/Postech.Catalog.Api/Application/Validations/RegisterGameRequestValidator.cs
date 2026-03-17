using ErrorOr;

namespace Postech.Catalog.Api.Application.Validations;

public static class RegisterGameRequestValidator
{
    public static ErrorOr<Success> Validate()
    {
        List<Error> errors = new List<Error>();
        
        //TODO: implementar validações de games        
        if (errors.Any())
        {
            return errors;
        }
        
        return Result.Success;
    }
}