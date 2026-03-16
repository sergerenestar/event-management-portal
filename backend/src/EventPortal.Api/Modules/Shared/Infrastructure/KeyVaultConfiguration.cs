using Azure.Identity;

namespace EventPortal.Api.Modules.Shared.Infrastructure;

public static class KeyVaultConfiguration
{
    public static void Configure(WebApplicationBuilder builder)
    {
        var keyVaultEndpoint = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"]
                               ?? Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT");

        if (!string.IsNullOrWhiteSpace(keyVaultEndpoint) && Uri.TryCreate(keyVaultEndpoint, UriKind.Absolute, out var vaultUri))
        {
            builder.Configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential());
        }
    }
}
