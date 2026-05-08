using EventPortal.Api.Modules.Events.Jobs;
using EventPortal.Api.Modules.Registrations.Jobs;
using Hangfire;

namespace EventPortal.Api.Modules.Shared.BackgroundJobs;

public static class JobRegistry
{
    public static void RegisterJobs(IApplicationBuilder app)
    {
        // event-sync — runs every hour (syncs event metadata + ticket types from Eventbrite)
        RecurringJob.AddOrUpdate<EventSyncJob>(
            "event-sync",
            j => j.ExecuteAsync(),
            Cron.Hourly);

        // registration-sync — runs every 15 minutes (pulls attendees/orders + updates snapshots)
        RecurringJob.AddOrUpdate<RegistrationSyncJob>(
            "registration-sync",
            j => j.ExecuteAsync(),
            "*/15 * * * *");

        // snapshot-aggregator — runs daily at midnight UTC (full recalculation safety net)
        RecurringJob.AddOrUpdate<SnapshotAggregatorJob>(
            "snapshot-aggregator",
            j => j.ExecuteAsync(),
            Cron.Daily);
    }
}
