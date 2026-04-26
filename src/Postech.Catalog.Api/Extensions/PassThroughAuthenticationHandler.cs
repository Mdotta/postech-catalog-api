using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
            TryGetTokenClaim("sub", out var tokenSub);
            TryGetTokenClaim("email", out var tokenEmail);
            TryGetTokenClaim("name", out var tokenName);

            // Extract user ID from header (set by API Gateway)
            if (Request.Headers.TryGetValue("X-User-Id", out var userId) && !string.IsNullOrWhiteSpace(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            }
            else if (!string.IsNullOrWhiteSpace(tokenSub))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, tokenSub));
            }

            // Prefer role from bearer token (source of truth); fallback to header only if token role is unavailable.
            if (TryGetRoleFromBearerToken(out var tokenRole))
            {
                claims.Add(new Claim(ClaimTypes.Role, tokenRole));
            }
            else if (Request.Headers.TryGetValue("X-User-Role", out var role) && !string.IsNullOrWhiteSpace(role))
            {
                if (TryResolveSingleRole(role.ToString(), out var resolvedHeaderRole))
                {
                    claims.Add(new Claim(ClaimTypes.Role, resolvedHeaderRole));
                }
            }

            // Extract username from header (set by API Gateway)
            if (Request.Headers.TryGetValue("X-User-Name", out var userName) && !string.IsNullOrWhiteSpace(userName))
            {
                claims.Add(new Claim(ClaimTypes.Name, userName.ToString()));
            }
            else if (!string.IsNullOrWhiteSpace(tokenName))
            {
                claims.Add(new Claim(ClaimTypes.Name, tokenName));
            }
            else if (!string.IsNullOrWhiteSpace(tokenEmail))
            {
                claims.Add(new Claim(ClaimTypes.Name, tokenEmail));
            }

            // If we have at least a user ID, create an authenticated principal
            if (claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
            {
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            // If no user ID header, report no authentication result so the
            // Authorization middleware treats the request as unauthenticated.
            // Returning Success with an empty identity would mark the
            // principal as authenticated (authentication type present),
            // which would incorrectly satisfy RequireAuthenticatedUser.
            return Task.FromResult(AuthenticateResult.NoResult());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during authentication");
            return Task.FromResult(AuthenticateResult.Fail(ex));
        }
    }

    private bool TryGetRoleFromBearerToken(out string role)
    {
        role = string.Empty;

        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return false;
        }

        var authValue = authHeader.ToString();
        if (string.IsNullOrWhiteSpace(authValue) || !authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = authValue[7..].Trim();
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            var root = ParseTokenPayload(parts[1]);
            var roles = new List<string>();

            if (root.TryGetProperty("cognito:groups", out var groupsElement))
            {
                if (groupsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in groupsElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var groupValue = item.GetString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(groupValue))
                        {
                            roles.Add(groupValue);
                        }
                    }
                }
                else if (groupsElement.ValueKind == JsonValueKind.String)
                {
                    var groupValue = groupsElement.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(groupValue))
                    {
                        roles.Add(groupValue);
                    }
                }
            }

            if (!roles.Any()
                && root.TryGetProperty("custom:role", out var customRole)
                && customRole.ValueKind == JsonValueKind.String)
            {
                var customRoleValue = customRole.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(customRoleValue))
                {
                    roles.Add(customRoleValue);
                }
            }

            if (!roles.Any()
                && root.TryGetProperty("role", out var roleElement)
                && roleElement.ValueKind == JsonValueKind.String)
            {
                var roleValue = roleElement.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(roleValue))
                {
                    roles.Add(roleValue);
                }
            }

            return TryResolveSingleRole(string.Join(',', roles), out role);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to extract role claim from bearer token");
            return false;
        }
    }

    private static bool TryResolveSingleRole(string rawRoles, out string role)
    {
        role = string.Empty;

        var normalizedRoles = rawRoles
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(r => r.Trim().Trim('[', ']', '"', '\''))
            .Select(NormalizeRole)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedRoles.Count == 0)
        {
            return false;
        }

        // One user, one effective role: choose Administrator if present, otherwise first role.
        role = normalizedRoles.FirstOrDefault(r => string.Equals(r, "Administrator", StringComparison.OrdinalIgnoreCase))
               ?? normalizedRoles[0];
        return true;
    }

    private bool TryGetTokenClaim(string claimName, out string claimValue)
    {
        claimValue = string.Empty;

        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return false;
        }

        var authValue = authHeader.ToString();
        if (string.IsNullOrWhiteSpace(authValue) || !authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = authValue[7..].Trim();
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            var root = ParseTokenPayload(parts[1]);
            if (!root.TryGetProperty(claimName, out var element))
            {
                return false;
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            claimValue = element.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(claimValue);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to extract claim {ClaimName} from bearer token", claimName);
            return false;
        }
    }

    private static JsonElement ParseTokenPayload(string payloadPart)
    {
        var payload = DecodeBase64Url(payloadPart);
        using var doc = JsonDocument.Parse(payload);
        return doc.RootElement.Clone();
    }

    private static string NormalizeRole(string role)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Administrator";
        }

        if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            return "User";
        }

        return role;
    }

    private static string DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        var mod = padded.Length % 4;
        if (mod > 0)
        {
            padded = padded.PadRight(padded.Length + (4 - mod), '=');
        }

        var bytes = Convert.FromBase64String(padded);
        return Encoding.UTF8.GetString(bytes);
    }
}

public class PassThroughAuthenticationOptions : AuthenticationSchemeOptions
{
}
