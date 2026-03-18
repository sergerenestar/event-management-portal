namespace EventPortal.Api.Modules.Auth.Dtos;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Lifetime of the access token in seconds (always 900 = 15 min).</summary>
    public int ExpiresIn { get; set; }

    public AdminUserDto Admin { get; set; } = new();
}
