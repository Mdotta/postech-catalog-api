using System.Security.Claims;
using Postech.Catalog.Api.Domain.Enums;

namespace Postech.Catalog.Api.Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsCurrentUserAdmin()
    {
        var role = GetCurrentUserRole();
        return role?.Equals(nameof(UserRoles.Administrator), StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public string? GetCurrentUserRole()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
    }
}

