using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EventPortal.Api.Modules.Auth.Entities;
using Microsoft.IdentityModel.Tokens;

namespace EventPortal.Api.Modules.Auth.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(AdminUser user)
    {
        var signingKey = _config["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var issuer   = _config["Jwt:Issuer"]    ?? "EventPortal";
        var audience = _config["Jwt:Audience"]  ?? "EventPortalClient";
        var expiry   = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 15;

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.DisplayName),
            new Claim(ClaimTypes.Role,               "Admin"),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Returns a cryptographically random 64-byte value as a Base64 string.
    /// The caller is responsible for hashing before persisting.
    /// </summary>
    public string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    /// <summary>
    /// SHA-256 hex hash of <paramref name="rawToken"/>. Deterministic — same input always
    /// produces the same output. Different inputs always produce different hashes.
    /// </summary>
    public string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Validates signature, issuer, audience, and lifetime.
    /// Returns the <see cref="ClaimsPrincipal"/> on success, or <c>null</c> on any failure.
    /// </summary>
    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        var signingKey = _config["Jwt:SigningKey"];
        if (string.IsNullOrEmpty(signingKey)) return null;

        var issuer   = _config["Jwt:Issuer"]   ?? "EventPortal";
        var audience = _config["Jwt:Audience"] ?? "EventPortalClient";

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = issuer,
            ValidAudience            = audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew                = TimeSpan.FromSeconds(30),
        };

        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }
}
