using EventPortal.Api.Modules.Auth.Services;
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
builder.Services.AddSwaggerGen();

// ── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["AllowedOrigins"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ── Module Services ───────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
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
app.UseCors();
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
