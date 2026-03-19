using EventPortal.Api.Modules.Events.Jobs;
using EventPortal.Api.Modules.Registrations.Jobs;
using Hangfire;

namespace EventPortal.Api.Modules.Shared.BackgroundJobs;

public static class JobRegistry
{
    public static void RegisterJobs(IApplicationBuilder app)
    {
        // event-sync — runs every hour
        RecurringJob.AddOrUpdate<EventSyncJob>(
            "event-sync",
            j => j.ExecuteAsync(),
            Cron.Hourly);

        // snapshot-aggregator — runs daily at midnight UTC
        RecurringJob.AddOrUpdate<SnapshotAggregatorJob>(
            "snapshot-aggregator",
            j => j.ExecuteAsync(),
            Cron.Daily);
    }
}
