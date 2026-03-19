using EventPortal.Api.Modules.Auth.Dtos;
using EventPortal.Api.Modules.Events.Integrations;
using EventPortal.Api.Modules.Events.Jobs;
using EventPortal.Api.Modules.Events.Repositories;
using EventPortal.Api.Modules.Registrations.Jobs;
using EventPortal.Api.Modules.Auth.Repositories;
using EventPortal.Api.Modules.Auth.Services;
using EventPortal.Api.Modules.Auth.Validators;
using FluentValidation;
using EventPortal.Api.Modules.AuditLogs.Services;
using EventPortal.Api.Modules.Campaigns.Services;
using EventPortal.Api.Modules.Events.Services;
using EventPortal.Api.Modules.Registrations.Services;
using EventPortal.Api.Modules.Reports.Services;
using EventPortal.Api.Modules.Sessions.Services;
using EventPortal.Api.Modules.Shared.BackgroundJobs;
using EventPortal.Api.Modules.Shared.Infrastructure;
using EventPortal.Api.Modules.Shared.Observability;
using EventPortal.Api.Modules.Shared.Persistence;
using EventPortal.Api.Modules.Shared.Security;
using EventPortal.Api.Modules.SocialPosts.Services;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Key Vault (must be first — loads secrets into IConfiguration) ─────────
KeyVaultConfiguration.Configure(builder);

// ── Serilog ───────────────────────────────────────────────────────────────
SerilogConfiguration.Configure(builder);

// ── Database ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ── Authentication / Authorization ───────────────────────────────────────
JwtConfiguration.Configure(builder);
AuthorizationPolicies.Configure(builder);

// ── Background Jobs (Hangfire) ────────────────────────────────────────────
HangfireConfiguration.Configure(builder);

// ── Application Insights ──────────────────────────────────────────────────
builder.Services.AddApplicationInsightsTelemetry();

// ── API ───────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger — JWT Bearer padlock ──────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Enter your JWT access token (without the 'Bearer ' prefix).",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer",
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // Required for HttpOnly cookie exchange
});

// ── Auth Repositories ─────────────────────────────────────────────────────
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// ── Auth Token Service ────────────────────────────────────────────────────
builder.Services.AddScoped<ITokenService, TokenService>();

// ── Entra Token Validator (singleton — shares OIDC key cache across requests) ──
builder.Services.AddSingleton<IEntraTokenValidator, EntraTokenValidator>();

// ── Eventbrite HTTP Client ────────────────────────────────────────────────
builder.Services.AddHttpClient<IEventbriteClient, EventbriteClient>();

// ── Event Repositories ────────────────────────────────────────────────────
builder.Services.AddScoped<IEventRepository, EventRepository>();

// ── Background Job Classes (resolved by Hangfire from DI) ─────────────────
builder.Services.AddScoped<EventSyncJob>();
builder.Services.AddScoped<SnapshotAggregatorJob>();

// ── Module Services ───────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IValidator<LoginRequestDto>, LoginRequestValidator>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<ISocialPostService, SocialPostService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────

// ── Middleware Pipeline ───────────────────────────────────────────────────
app.UseCorrelationId();
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");  // Must be before UseAuthentication for cookies to work
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            environment = app.Environment.EnvironmentName,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [] // Secured in production via policy — open in dev
});

JobRegistry.RegisterJobs(app);

app.Run();
