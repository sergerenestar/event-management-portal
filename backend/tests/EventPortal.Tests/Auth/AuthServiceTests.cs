using System.Security.Claims;
using EventPortal.Api.Modules.Auth.Dtos;
using EventPortal.Api.Modules.Auth.Entities;
using EventPortal.Api.Modules.Auth.Repositories;
using EventPortal.Api.Modules.Auth.Services;
using EventPortal.Api.Modules.AuditLogs.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EventPortal.Tests.Auth;

public class AuthServiceTests
{
    // ── Mocks ─────────────────────────────────────────────────────────────
    private readonly Mock<IAdminUserRepository>    _users          = new();
    private readonly Mock<IRefreshTokenRepository> _tokens         = new();
    private readonly Mock<ITokenService>           _tokenService   = new();
    private readonly Mock<IEntraTokenValidator>    _entraValidator = new();
    private readonly Mock<IAuditLogger>            _audit          = new();

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ExpiryMinutes"] = "15",
            })
            .Build();

        _sut = new AuthService(
            _users.Object,
            _tokens.Object,
            _tokenService.Object,
            _entraValidator.Object,
            _audit.Object,
            config);

        // Default token service behaviour — all tests can override as needed
        _tokenService
            .Setup(t => t.GenerateAccessToken(It.IsAny<AdminUser>()))
            .Returns("fake-access-token");

        _tokenService
            .Setup(t => t.GenerateRefreshToken())
            .Returns("raw-refresh-token");

        _tokenService
            .Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns<string>(s => $"hash-of:{s}");

        // Audit logger never throws
        _audit
            .Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static ClaimsPrincipal BuildEntraPrincipal(
        string oid   = "entra-oid-001",
        string email = "admin@example.com",
        string name  = "Test Admin")
    {
        var claims = new[]
        {
            new Claim("oid",                oid),
            new Claim("preferred_username", email),
            new Claim("name",               name),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    private static LoginRequestDto MakeLoginRequest(string provider = "Microsoft") =>
        new() { EntraIdToken = "entra-id-token", Provider = provider };

    // ── LoginAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_NewUser_CallsCreateAndPersistsRefreshToken()
    {
        // Arrange
        var principal = BuildEntraPrincipal();
        var newUser   = new AdminUser
        {
            Id = 1, Email = "admin@example.com",
            DisplayName = "Test Admin", IsActive = true,
            ExternalObjectId = "entra-oid-001",
        };

        _entraValidator
            .Setup(v => v.ValidateAsync("entra-id-token"))
            .ReturnsAsync(principal);

        _users.Setup(u => u.GetByExternalObjectIdAsync("entra-oid-001"))
              .ReturnsAsync((AdminUser?)null);

        _users.Setup(u => u.CreateAsync(It.IsAny<AdminUser>()))
              .ReturnsAsync(newUser);

        // Act
        var (result, rawToken) = await _sut.LoginAsync(MakeLoginRequest());

        // Assert — CreateAsync called with correct fields
        _users.Verify(u => u.CreateAsync(It.Is<AdminUser>(a =>
            a.Email            == "admin@example.com" &&
            a.IdentityProvider == "Microsoft"         &&
            a.ExternalObjectId == "entra-oid-001")),
            Times.Once);

        // Assert — refresh token persisted with hash, not raw value
        _tokens.Verify(t => t.CreateAsync(It.Is<RefreshToken>(r =>
            r.AdminUserId == 1 &&
            r.TokenHash   == "hash-of:raw-refresh-token" &&
            r.ExpiresAt   >  DateTime.UtcNow)),
            Times.Once);

        result.AccessToken.Should().Be("fake-access-token");
        result.ExpiresIn.Should().Be(900);
        result.Admin.Id.Should().Be(1);
        rawToken.Should().Be("raw-refresh-token");
    }

    [Fact]
    public async Task LoginAsync_ReturningUser_CallsUpdateLastLoginAndDoesNotCallCreate()
    {
        // Arrange
        var existingUser = new AdminUser
        {
            Id = 2, Email = "admin@example.com",
            IsActive = true, ExternalObjectId = "entra-oid-001",
        };

        _entraValidator
            .Setup(v => v.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(BuildEntraPrincipal());

        _users.Setup(u => u.GetByExternalObjectIdAsync("entra-oid-001"))
              .ReturnsAsync(existingUser);

        // Act
        await _sut.LoginAsync(MakeLoginRequest());

        // Assert
        _users.Verify(u => u.UpdateLastLoginAsync(2), Times.Once);
        _users.Verify(u => u.CreateAsync(It.IsAny<AdminUser>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_DisabledUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var disabledUser = new AdminUser
        {
            Id = 3, IsActive = false, ExternalObjectId = "entra-oid-001",
        };

        _entraValidator
            .Setup(v => v.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(BuildEntraPrincipal());

        _users.Setup(u => u.GetByExternalObjectIdAsync("entra-oid-001"))
              .ReturnsAsync(disabledUser);

        // Act
        var act = () => _sut.LoginAsync(MakeLoginRequest());

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*disabled*");
    }

    // ── RefreshAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_ValidToken_RevokesOldAndReturnsNewTokenPair()
    {
        // Arrange
        var stored = new RefreshToken { Id = 10, AdminUserId = 5 };
        var user   = new AdminUser   { Id = 5, IsActive = true };

        _tokens.Setup(t => t.GetByHashAsync("hash-of:incoming-token"))
               .ReturnsAsync(stored);

        _users.Setup(u => u.GetByIdAsync(5))
              .ReturnsAsync(user);

        // Act
        var (result, newRaw) = await _sut.RefreshAsync("incoming-token");

        // Assert — old token immediately revoked (rotation)
        _tokens.Verify(t => t.RevokeAsync(10), Times.Once);

        // Assert — new hashed refresh token persisted
        _tokens.Verify(t => t.CreateAsync(It.Is<RefreshToken>(r =>
            r.AdminUserId == 5 &&
            r.TokenHash   == "hash-of:raw-refresh-token")),
            Times.Once);

        result.AccessToken.Should().Be("fake-access-token");
        result.ExpiresIn.Should().Be(900);
        newRaw.Should().Be("raw-refresh-token");
    }

    [Fact]
    public async Task RefreshAsync_RevokedOrExpiredToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange — GetByHashAsync returns null (revoked/expired filtered at repository level)
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>()))
               .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = () => _sut.RefreshAsync("stale-token");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── LogoutAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesAllUserTokensAndWritesAuditLog()
    {
        // Arrange
        var stored = new RefreshToken { Id = 7, AdminUserId = 9 };

        _tokens.Setup(t => t.GetByHashAsync("hash-of:logout-token"))
               .ReturnsAsync(stored);

        // Act
        await _sut.LogoutAsync("logout-token");

        // Assert
        _tokens.Verify(t => t.RevokeAllForUserAsync(9), Times.Once);
        _audit.Verify(a => a.LogAsync("Logout", 9, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_AlreadyExpiredToken_CompletesWithoutError()
    {
        // Arrange — token not found (already revoked/expired)
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>()))
               .ReturnsAsync((RefreshToken?)null);

        // Act — should not throw
        var act = () => _sut.LogoutAsync("unknown-token");

        await act.Should().NotThrowAsync();
        _tokens.Verify(t => t.RevokeAllForUserAsync(It.IsAny<int>()), Times.Never);
    }
}
