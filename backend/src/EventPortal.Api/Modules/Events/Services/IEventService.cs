using EventPortal.Api.Modules.Events.Entities;

namespace EventPortal.Api.Modules.Events.Services;

public interface IEventService
{
    Task<List<Event>> GetEventsAsync();
    Task<Event?> GetEventByIdAsync(int id);
    Task SyncEventsAsync();
    Task SyncEventDetailAsync(int eventId);
}
