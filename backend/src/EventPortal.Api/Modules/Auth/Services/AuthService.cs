using System.Security.Claims;
using EventPortal.Api.Modules.Auth.Dtos;
using EventPortal.Api.Modules.Auth.Entities;
using EventPortal.Api.Modules.Auth.Repositories;
using EventPortal.Api.Modules.AuditLogs.Services;

namespace EventPortal.Api.Modules.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IAdminUserRepository    _users;
    private readonly IRefreshTokenRepository _tokens;
    private readonly ITokenService           _tokenService;
    private readonly IEntraTokenValidator    _entraValidator;
    private readonly IAuditLogger            _audit;
    private readonly IConfiguration          _config;

    public AuthService(
        IAdminUserRepository    users,
        IRefreshTokenRepository tokens,
        ITokenService           tokenService,
        IEntraTokenValidator    entraValidator,
        IAuditLogger            audit,
        IConfiguration          config)
    {
        _users          = users;
        _tokens         = tokens;
        _tokenService   = tokenService;
        _entraValidator = entraValidator;
        _audit          = audit;
        _config         = config;
    }

    // ── Login ──────────────────────────────────────────────────────────────

    public async Task<(LoginResponseDto result, string rawRefreshToken)> LoginAsync(LoginRequestDto request)
    {
        // 1. Validate the Entra External ID token
        var principal = await _entraValidator.ValidateAsync(request.EntraIdToken);

        // 2. Extract identity claims — oid is the stable, immutable Entra object ID
        var externalObjectId =
            principal.FindFirstValue("oid") ??
            principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
            throw new UnauthorizedAccessException("Entra token is missing the 'oid' identity claim.");

        var email =
            principal.FindFirstValue("preferred_username") ??
            principal.FindFirstValue(ClaimTypes.Email) ??
            throw new UnauthorizedAccessException("Entra token is missing an email claim.");

        var displayName =
            principal.FindFirstValue("name") ??
            principal.FindFirstValue(ClaimTypes.Name) ??
            email;

        // 3. Upsert AdminUser
        var adminUser = await _users.GetByExternalObjectIdAsync(externalObjectId);
        if (adminUser is null)
        {
            adminUser = await _users.CreateAsync(new AdminUser
            {
                Email            = email,
                DisplayName      = displayName,
                IdentityProvider = request.Provider,
                ExternalObjectId = externalObjectId,
            });
        }
        else
        {
            await _users.UpdateLastLoginAsync(adminUser.Id);
        }

        // 4. Reject disabled accounts after the upsert
        if (!adminUser.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        // 5–6. Issue tokens and persist hashed refresh token
        var (accessToken, rawRefreshToken) = await IssueTokenPairAsync(adminUser);

        // 7. Audit
        await _audit.LogAsync("Login", adminUser.Id, $"Provider:{request.Provider}");

        return (new LoginResponseDto
        {
            AccessToken = accessToken,
            ExpiresIn   = AccessTokenExpirySeconds(),
            Admin       = MapToDto(adminUser),
        }, rawRefreshToken);
    }

    // ── Refresh ────────────────────────────────────────────────────────────

    public async Task<(RefreshResponseDto result, string rawRefreshToken)> RefreshAsync(string rawRefreshToken)
    {
        // 1. Hash to look up stored token
        var hash = _tokenService.HashToken(rawRefreshToken);

        // 2. GetByHashAsync already filters IsRevoked=false and ExpiresAt>UtcNow
        var storedToken = await _tokens.GetByHashAsync(hash)
            ?? throw new UnauthorizedAccessException("Refresh token is invalid, expired, or already used.");

        // 3. Rotate — revoke immediately before issuing the replacement
        await _tokens.RevokeAsync(storedToken.Id);

        var adminUser = await _users.GetByIdAsync(storedToken.AdminUserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!adminUser.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        // 4–5. Issue new token pair
        var (newAccessToken, newRawRefreshToken) = await IssueTokenPairAsync(adminUser);

        // 6. Audit
        await _audit.LogAsync("TokenRefresh", adminUser.Id);

        return (new RefreshResponseDto
        {
            AccessToken = newAccessToken,
            ExpiresIn   = AccessTokenExpirySeconds(),
        }, newRawRefreshToken);
    }

    // ── Me ─────────────────────────────────────────────────────────────────

    public async Task<AdminUserDto?> GetMeAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user is null) return null;
        // Returns DTO regardless of IsActive — controller decides 200 vs 403
        return MapToDto(user);
    }

    // ── Logout ─────────────────────────────────────────────────────────────

    public async Task LogoutAsync(string rawRefreshToken)
    {
        var hash  = _tokenService.HashToken(rawRefreshToken);
        var token = await _tokens.GetByHashAsync(hash);
        if (token is null) return; // already revoked / expired — treat as success

        await _tokens.RevokeAllForUserAsync(token.AdminUserId);
        await _audit.LogAsync("Logout", token.AdminUserId);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates an access + raw refresh token and persists the hashed refresh token.
    /// Takes the already-loaded <paramref name="adminUser"/> to avoid an extra DB round-trip.
    /// </summary>
    private async Task<(string accessToken, string rawRefreshToken)> IssueTokenPairAsync(AdminUser adminUser)
    {
        var accessToken     = _tokenService.GenerateAccessToken(adminUser);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        await _tokens.CreateAsync(new RefreshToken
        {
            AdminUserId = adminUser.Id,
            TokenHash   = _tokenService.HashToken(rawRefreshToken),
            ExpiresAt   = DateTime.UtcNow.AddDays(7),
        });

        return (accessToken, rawRefreshToken);
    }

    private int AccessTokenExpirySeconds()
    {
        var minutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 15;
        return minutes * 60;
    }

    private static AdminUserDto MapToDto(AdminUser user) => new()
    {
        Id          = user.Id,
        Email       = user.Email,
        DisplayName = user.DisplayName,
        IsActive    = user.IsActive,
    };
}
