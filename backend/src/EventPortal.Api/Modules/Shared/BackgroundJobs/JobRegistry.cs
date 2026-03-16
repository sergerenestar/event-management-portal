namespace EventPortal.Api.Modules.Shared.BackgroundJobs;

// All Hangfire recurring jobs are declared here.
// Feature sprints add their jobs to this registry.
public static class JobRegistry
{
    public static void RegisterJobs(IApplicationBuilder app)
    {
        // Sprint 2+: Register recurring sync jobs here
        // Example:
        // var manager = new RecurringJobManager();
        // manager.AddOrUpdate<EventSyncJob>("event-sync", j => j.ExecuteAsync(CancellationToken.None), "0 */6 * * *");
    }
}
