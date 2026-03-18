namespace EventPortal.Api.Modules.Auth.Dtos;

public class LoginRequestDto
{
    /// <summary>OIDC ID token issued by Microsoft Entra External ID after MSAL login.</summary>
    public string EntraIdToken { get; set; } = string.Empty;

    /// <summary>"Microsoft" or "Google"</summary>
    public string Provider { get; set; } = string.Empty;
}
