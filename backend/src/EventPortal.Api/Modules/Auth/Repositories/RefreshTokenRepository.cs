using EventPortal.Api.Modules.Auth.Entities;
using EventPortal.Api.Modules.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPortal.Api.Modules.Auth.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t =>
            t.TokenHash == tokenHash &&
            !t.IsRevoked &&
            t.ExpiresAt > DateTime.UtcNow);

    public async Task CreateAsync(RefreshToken token)
    {
        token.CreatedAt = DateTime.UtcNow;
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
    }

    public async Task RevokeAsync(int tokenId)
    {
        await _db.RefreshTokens
            .Where(t => t.Id == tokenId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true));
    }

    public async Task RevokeAllForUserAsync(int adminUserId)
    {
        await _db.RefreshTokens
            .Where(t => t.AdminUserId == adminUserId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true));
    }
}
