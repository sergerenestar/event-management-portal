using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EventPortal.Api.Modules.Auth.Entities;
using EventPortal.Api.Modules.Auth.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace EventPortal.Tests.Auth;

public class TokenServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────

    private const string SigningKey = "test-signing-key-32-characters!!"; // exactly 32 chars
    private const string Issuer     = "EventPortal";
    private const string Audience   = "EventPortalClient";

    private readonly TokenService _sut;

    private readonly AdminUser _user = new()
    {
        Id          = 42,
        Email       = "admin@example.com",
        DisplayName = "Test Admin",
        IsActive    = true,
    };

    public TokenServiceTests()
    {
        _sut = BuildService(expiryMinutes: 15);
    }

    private static TokenService BuildService(int expiryMinutes = 15)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"]     = SigningKey,
                ["Jwt:Issuer"]        = Issuer,
                ["Jwt:Audience"]      = Audience,
                ["Jwt:ExpiryMinutes"] = expiryMinutes.ToString(),
            })
            .Build();
        return new TokenService(config);
    }

    // ── GenerateAccessToken ───────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var token = _sut.GenerateAccessToken(_user);
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        var token = _sut.GenerateAccessToken(_user);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // sub
        jwt.Subject.Should().Be("42");

        // email
        jwt.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Email && c.Value == "admin@example.com");

        // name
        jwt.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Name && c.Value == "Test Admin");

        // role — JwtSecurityTokenHandler maps ClaimTypes.Role outbound to "role"
        jwt.Claims.Should().Contain(c =>
            (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == "Admin");
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectIssuerAndAudience()
    {
        var token = _sut.GenerateAccessToken(_user);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be(Issuer);
        jwt.Audiences.Should().Contain(Audience);
    }

    // ── GenerateRefreshToken ──────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var token = _sut.GenerateRefreshToken();
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_TwoCallsProduceDifferentValues()
    {
        var first  = _sut.GenerateRefreshToken();
        var second = _sut.GenerateRefreshToken();

        first.Should().NotBe(second);
    }

    // ── HashToken ─────────────────────────────────────────────────────────

    [Fact]
    public void HashToken_SameInput_AlwaysReturnsSameHash()
    {
        const string raw = "some-raw-token-value";

        var hash1 = _sut.HashToken(raw);
        var hash2 = _sut.HashToken(raw);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_DifferentInputs_ProduceDifferentHashes()
    {
        var hash1 = _sut.HashToken("token-a");
        var hash2 = _sut.HashToken("token-b");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashToken_ReturnsSha256HexString_64Chars()
    {
        var hash = _sut.HashToken("any-value");

        // SHA-256 produces 32 bytes = 64 hex characters
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    // ── ValidateAccessToken ───────────────────────────────────────────────

    [Fact]
    public void ValidateAccessToken_ValidToken_ReturnsPrincipalWithCorrectSub()
    {
        var token     = _sut.GenerateAccessToken(_user);
        var principal = _sut.ValidateAccessToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirstValue(ClaimTypes.NameIdentifier)
            .Should().Be("42");
    }

    [Fact]
    public void ValidateAccessToken_TamperedSignature_ReturnsNull()
    {
        var token  = _sut.GenerateAccessToken(_user);
        var parts  = token.Split('.');

        // Corrupt the last 4 characters of the signature segment
        parts[2] = parts[2][..^4] + "XXXX";
        var tampered = string.Join('.', parts);

        var principal = _sut.ValidateAccessToken(tampered);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_ExpiredToken_ReturnsNull()
    {
        // Build a service with -2 minute expiry — token is issued already 2 min in the past,
        // well beyond the 30-second ClockSkew tolerance
        var expiredSvc   = BuildService(expiryMinutes: -2);
        var expiredToken = expiredSvc.GenerateAccessToken(_user);

        // Validate with the standard service (same signing key, same issuer/audience)
        var principal = _sut.ValidateAccessToken(expiredToken);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_WrongSigningKey_ReturnsNull()
    {
        // Token signed with a different key
        var otherConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"]     = "different-signing-key-32-chars!!",
                ["Jwt:Issuer"]        = Issuer,
                ["Jwt:Audience"]      = Audience,
                ["Jwt:ExpiryMinutes"] = "15",
            })
            .Build();
        var otherSvc   = new TokenService(otherConfig);
        var otherToken = otherSvc.GenerateAccessToken(_user);

        var principal = _sut.ValidateAccessToken(otherToken);

        principal.Should().BeNull();
    }
}
