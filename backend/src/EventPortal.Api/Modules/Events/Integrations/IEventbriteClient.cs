namespace EventPortal.Api.Modules.Events.Integrations;

public interface IEventbriteClient
{
    Task<List<EventbriteEvent>> GetEventsAsync(string organizationId);
    Task<List<EventbriteTicketClass>> GetTicketClassesAsync(string eventId);
    Task<List<EventbriteOrder>> GetOrdersAsync(string eventId);
    Task<List<EventbriteAttendee>> GetAttendeesAsync(string eventId);
}
