using EventPortal.Api.Modules.Events.Entities;

namespace EventPortal.Api.Modules.Events.Repositories;

public interface IEventRepository
{
    Task<List<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(int id);
    Task<Event?> GetByExternalIdAsync(string externalId);
    Task UpsertAsync(Event ev);
    Task UpsertTicketTypeAsync(TicketType ticketType);
}
