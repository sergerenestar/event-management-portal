using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EventPortal.Api.Modules.Shared.Security;

public static class JwtConfiguration
{
    public static void Configure(WebApplicationBuilder builder)
    {
        var signingKey = builder.Configuration["Jwt:SigningKey"]
                         ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var issuer   = builder.Configuration["Jwt:Issuer"]   ?? "EventPortal";
        var audience = builder.Configuration["Jwt:Audience"] ?? "EventPortalClient";

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
    }
}
