using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Postech.Catalog.Api.Extensions;

/// <summary>
/// Pass-through authentication handler that extracts claims from headers set by API Gateway (Cognito)
/// JWT validation is delegated to the API Gateway, this handler only extracts claims for policy checks
/// </summary>
public class PassThroughAuthenticationHandler : AuthenticationHandler<PassThroughAuthenticationOptions>
{
    public PassThroughAuthenticationHandler(
        IOptionsMonitor<PassThroughAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var claims = new List<Claim>();

            // Extract user ID from header (set by API Gateway)
            if (Request.Headers.TryGetValue("X-User-Id", out var userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            }

            // Extract user role from header (set by API Gateway)
            if (Request.Headers.TryGetValue("X-User-Role", out var role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }

            // Extract username from header (set by API Gateway)
            if (Request.Headers.TryGetValue("X-User-Name", out var userName))
            {
                claims.Add(new Claim(ClaimTypes.Name, userName.ToString()));
            }

            // If we have at least a user ID, create an authenticated principal
            if (claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
            {
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            // If no user ID header, return success with anonymous principal
            // This allows unauthenticated requests to pass through
            var anonIdentity = new ClaimsIdentity(claims, Scheme.Name);
            var anonPrincipal = new ClaimsPrincipal(anonIdentity);
            var anonTicket = new AuthenticationTicket(anonPrincipal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(anonTicket));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during authentication");
            return Task.FromResult(AuthenticateResult.Fail(ex));
        }
    }
}

public class PassThroughAuthenticationOptions : AuthenticationSchemeOptions
{
}
