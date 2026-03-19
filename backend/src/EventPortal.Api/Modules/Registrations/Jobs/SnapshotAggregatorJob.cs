using EventPortal.Api.Modules.Events.Repositories;
using EventPortal.Api.Modules.Registrations.Services;
using Microsoft.Extensions.Logging;

namespace EventPortal.Api.Modules.Registrations.Jobs;

public class SnapshotAggregatorJob
{
    private readonly IRegistrationService _registrationService;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<SnapshotAggregatorJob> _logger;

    public SnapshotAggregatorJob(
        IRegistrationService registrationService,
        IEventRepository eventRepository,
        ILogger<SnapshotAggregatorJob> logger)
    {
        _registrationService = registrationService;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("SnapshotAggregatorJob started");

        var events = await _eventRepository.GetAllAsync();

        foreach (var ev in events)
        {
            try
            {
                await _registrationService.AggregateSnapshotsAsync(ev.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SnapshotAggregatorJob failed for event {EventId}", ev.Id);
            }
        }

        _logger.LogInformation("SnapshotAggregatorJob completed. {Count} events processed.", events.Count);
    }
}
