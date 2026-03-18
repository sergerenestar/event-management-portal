namespace EventPortal.Api.Modules.Auth.Dtos;

public class RefreshResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Lifetime of the new access token in seconds (always 900 = 15 min).</summary>
    public int ExpiresIn { get; set; }
}
