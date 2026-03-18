using EventPortal.Api.Modules.Auth.Entities;

namespace EventPortal.Api.Modules.Auth.Repositories;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByExternalObjectIdAsync(string externalObjectId);
    Task<AdminUser?> GetByIdAsync(int id);
    Task<AdminUser> CreateAsync(AdminUser user);
    Task UpdateLastLoginAsync(int userId);
}
