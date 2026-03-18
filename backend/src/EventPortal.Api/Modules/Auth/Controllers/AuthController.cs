using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EventPortal.Api.Modules.Auth.Dtos;
using EventPortal.Api.Modules.Auth.Services;
using EventPortal.Api.Modules.Shared.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPortal.Api.Modules.Auth.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "ep_refresh";
    private const string RefreshCookiePath = "/api/v1/auth";

    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequestDto> _validator;

    public AuthController(IAuthService authService, IValidator<LoginRequestDto> validator)
    {
        _authService = authService;
        _validator   = validator;
    }

    // ── POST /api/v1/auth/login ────────────────────────────────────────────

    /// <summary>
    /// Exchanges an Entra External ID token for a portal JWT + HttpOnly refresh cookie.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var (result, rawRefreshToken) = await _authService.LoginAsync(request);

        SetRefreshCookie(rawRefreshToken);
        return Ok(result);
    }

    // ── POST /api/v1/auth/refresh ──────────────────────────────────────────

    /// <summary>
    /// Rotates the refresh token and issues a new access token.
    /// The refresh token is read from and written back to an HttpOnly cookie.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var rawToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(rawToken))
            return Unauthorized(new { message = "Refresh token cookie is missing." });

        var (result, newRawRefreshToken) = await _authService.RefreshAsync(rawToken);

        SetRefreshCookie(newRawRefreshToken);
        return Ok(result);
    }

    // ── GET /api/v1/auth/me ────────────────────────────────────────────────

    /// <summary>
    /// Returns the authenticated admin's profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "User ID claim is missing from token." });

        var admin = await _authService.GetMeAsync(userId);
        if (admin is null)
            return NotFound(new { message = "User not found." });

        if (!admin.IsActive)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Account is disabled." });

        return Ok(admin);
    }

    // ── POST /api/v1/auth/logout ───────────────────────────────────────────

    /// <summary>
    /// Revokes all refresh tokens for the authenticated admin and clears the cookie.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var rawToken = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(rawToken))
            await _authService.LogoutAsync(rawToken);

        DeleteRefreshCookie();
        return Ok(new { message = "Logged out." });
    }

    // ── Cookie helpers ─────────────────────────────────────────────────────

    private void SetRefreshCookie(string rawToken)
    {
        Response.Cookies.Append(RefreshCookieName, rawToken, new CookieOptions
        {
            HttpOnly  = true,
            Secure    = true,
            SameSite  = SameSiteMode.Strict,
            Expires   = DateTimeOffset.UtcNow.AddDays(7),
            Path      = RefreshCookiePath,
        });
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Path     = RefreshCookiePath,
        });
    }

    // ── Claim helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Reads the <c>sub</c> claim from the JWT. ASP.NET Core's JwtBearer middleware maps
    /// the JWT <c>sub</c> claim to <see cref="ClaimTypes.NameIdentifier"/> by default.
    /// </summary>
    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return raw is not null && int.TryParse(raw, out userId);
    }
}
