namespace EventPortal.Api.Modules.Shared.Security;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";

    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminOnly, policy =>
                policy.RequireAuthenticatedUser());
        });
    }
}
