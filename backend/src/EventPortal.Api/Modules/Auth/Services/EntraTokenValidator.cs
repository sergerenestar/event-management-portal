using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace EventPortal.Api.Modules.Auth.Services;

/// <summary>
/// Singleton: the <see cref="ConfigurationManager{T}"/> caches OIDC signing keys
/// across requests, avoiding a round-trip to Microsoft on every login.
/// </summary>
public class EntraTokenValidator : IEntraTokenValidator
{
    private readonly IConfiguration _config;
    private readonly ILogger<EntraTokenValidator> _logger;

    // Lazily initialised on first login; shared for the lifetime of the application.
    private ConfigurationManager<OpenIdConnectConfiguration>? _oidcConfigManager;

    public EntraTokenValidator(IConfiguration config, ILogger<EntraTokenValidator> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> ValidateAsync(string idToken)
    {
        var tenantId = _config["Entra:TenantId"]
            ?? throw new InvalidOperationException("Entra:TenantId is not configured.");
        var clientId = _config["Entra:ClientId"]
            ?? throw new InvalidOperationException("Entra:ClientId is not configured.");
        var audience = _config["Entra:Audience"] ?? clientId;

        _oidcConfigManager ??= new ConfigurationManager<OpenIdConnectConfiguration>(
            $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

        var oidcConfig = await _oidcConfigManager.GetConfigurationAsync();

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuers             =
            [
                $"https://login.microsoftonline.com/{tenantId}/v2.0",
                $"https://sts.windows.net/{tenantId}/",
            ],
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateLifetime         = true,
            IssuerSigningKeys        = oidcConfig.SigningKeys,
            ClockSkew                = TimeSpan.FromMinutes(5),
        };

        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(idToken, validationParams, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Entra token validation failed: {Message}", ex.Message);
            throw new UnauthorizedAccessException("The Entra ID token could not be validated.");
        }
    }
}
