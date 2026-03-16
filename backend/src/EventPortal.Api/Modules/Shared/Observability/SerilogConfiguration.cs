using Serilog;
using Serilog.Events;

namespace EventPortal.Api.Modules.Shared.Observability;

public static class SerilogConfiguration
{
    public static void Configure(WebApplicationBuilder builder)
    {
        var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");

        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            loggerConfig.WriteTo.ApplicationInsights(
                appInsightsConnectionString,
                TelemetryConverter.Traces);
        }

        Log.Logger = loggerConfig.CreateLogger();

        builder.Host.UseSerilog();
    }
}
