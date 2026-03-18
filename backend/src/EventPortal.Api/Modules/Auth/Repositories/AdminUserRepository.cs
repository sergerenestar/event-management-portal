using EventPortal.Api.Modules.Auth.Entities;
using EventPortal.Api.Modules.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPortal.Api.Modules.Auth.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly AppDbContext _db;

    public AdminUserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<AdminUser?> GetByExternalObjectIdAsync(string externalObjectId) =>
        _db.AdminUsers.FirstOrDefaultAsync(u => u.ExternalObjectId == externalObjectId);

    public Task<AdminUser?> GetByIdAsync(int id) =>
        _db.AdminUsers.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<AdminUser> CreateAsync(AdminUser user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;
        _db.AdminUsers.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        await _db.AdminUsers
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTime.UtcNow));
    }
}
