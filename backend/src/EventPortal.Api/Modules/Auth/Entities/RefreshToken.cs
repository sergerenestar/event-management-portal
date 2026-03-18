namespace EventPortal.Api.Modules.Auth.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int AdminUserId { get; set; }

    /// <summary>SHA-256 hex hash of the raw token. Raw value is never stored.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public AdminUser AdminUser { get; set; } = null!;
}
