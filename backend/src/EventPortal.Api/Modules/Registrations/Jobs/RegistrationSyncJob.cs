using EventPortal.Api.Modules.Events.Repositories;
using EventPortal.Api.Modules.Registrations.Services;
using Microsoft.Extensions.Logging;

namespace EventPortal.Api.Modules.Registrations.Jobs;

public class RegistrationSyncJob
{
    private readonly IRegistrationService _registrationService;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<RegistrationSyncJob> _logger;

    public RegistrationSyncJob(
        IRegistrationService registrationService,
        IEventRepository eventRepository,
        ILogger<RegistrationSyncJob> logger)
    {
        _registrationService = registrationService;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("RegistrationSyncJob started");

        var events = await _eventRepository.GetAllAsync();

        foreach (var ev in events)
        {
            try
            {
                await _registrationService.SyncRegistrationsAsync(ev.Id);
                await _registrationService.AggregateSnapshotsAsync(ev.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RegistrationSyncJob failed for event {EventId}", ev.Id);
            }
        }

        _logger.LogInformation("RegistrationSyncJob completed. {Count} events processed.", events.Count);
    }
}
