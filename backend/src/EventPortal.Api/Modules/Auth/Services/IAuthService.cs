using EventPortal.Api.Modules.Auth.Dtos;

namespace EventPortal.Api.Modules.Auth.Services;

public interface IAuthService
{
    Task<(LoginResponseDto result, string rawRefreshToken)> LoginAsync(LoginRequestDto request);
    Task<(RefreshResponseDto result, string rawRefreshToken)> RefreshAsync(string rawRefreshToken);
    Task<AdminUserDto?> GetMeAsync(int userId);
    Task LogoutAsync(string rawRefreshToken);
}
