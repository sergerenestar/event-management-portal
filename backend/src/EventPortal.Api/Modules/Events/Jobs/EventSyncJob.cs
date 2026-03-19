using EventPortal.Api.Modules.Events.Services;
using Microsoft.Extensions.Logging;

namespace EventPortal.Api.Modules.Events.Jobs;

public class EventSyncJob
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventSyncJob> _logger;

    public EventSyncJob(IEventService eventService, ILogger<EventSyncJob> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("EventSyncJob started");
        await _eventService.SyncEventsAsync();
        _logger.LogInformation("EventSyncJob completed");
    }
}
