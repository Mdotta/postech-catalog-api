namespace Postech.Catalog.Api.Application.Services;

public interface IAuthorizationService
{
    bool IsCurrentUserAdmin();
    Guid? GetCurrentUserId();
    string? GetCurrentUserRole();
}

