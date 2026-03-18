using System.Security.Claims;
using EventPortal.Api.Modules.Auth.Entities;

namespace EventPortal.Api.Modules.Auth.Services;

public interface ITokenService
{
    string GenerateAccessToken(AdminUser user);
    string GenerateRefreshToken();
    string HashToken(string rawToken);
    ClaimsPrincipal? ValidateAccessToken(string token);
}
