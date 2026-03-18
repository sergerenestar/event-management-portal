using System.Security.Claims;

namespace EventPortal.Api.Modules.Auth.Services;

public interface IEntraTokenValidator
{
    /// <summary>
    /// Validates an Entra External ID OIDC ID token against the configured tenant.
    /// Returns the <see cref="ClaimsPrincipal"/> on success.
    /// Throws <see cref="UnauthorizedAccessException"/> on any validation failure.
    /// </summary>
    Task<ClaimsPrincipal> ValidateAsync(string idToken);
}
