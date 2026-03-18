using EventPortal.Api.Modules.Auth.Entities;

namespace EventPortal.Api.Modules.Auth.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task CreateAsync(RefreshToken token);
    Task RevokeAsync(int tokenId);
    Task RevokeAllForUserAsync(int adminUserId);
}
