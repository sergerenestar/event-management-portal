using EventPortal.Api.Modules.Events.Entities;
using EventPortal.Api.Modules.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventPortal.Api.Modules.Events.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;

    public EventRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Event>> GetAllAsync() =>
        _db.Events
           .Include(e => e.TicketTypes)
           .OrderByDescending(e => e.StartDate)
           .ToListAsync();

    public Task<Event?> GetByIdAsync(int id) =>
        _db.Events
           .Include(e => e.TicketTypes)
           .FirstOrDefaultAsync(e => e.Id == id);

    public Task<Event?> GetByExternalIdAsync(string externalId) =>
        _db.Events.FirstOrDefaultAsync(e => e.ExternalEventbriteId == externalId);

    public async Task UpsertAsync(Event ev)
    {
        var existing = await _db.Events
            .FirstOrDefaultAsync(e => e.ExternalEventbriteId == ev.ExternalEventbriteId);

        if (existing is null)
        {
            ev.CreatedAt = DateTime.UtcNow;
            ev.UpdatedAt = DateTime.UtcNow;
            _db.Events.Add(ev);
        }
        else
        {
            existing.Name = ev.Name;
            existing.Slug = ev.Slug;
            existing.StartDate = ev.StartDate;
            existing.EndDate = ev.EndDate;
            existing.Venue = ev.Venue;
            existing.Status = ev.Status;
            existing.ThumbnailUrl = ev.ThumbnailUrl;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpsertTicketTypeAsync(TicketType ticketType)
    {
        var existing = await _db.TicketTypes
            .FirstOrDefaultAsync(t =>
                t.ExternalTicketClassId == ticketType.ExternalTicketClassId &&
                t.EventId == ticketType.EventId);

        if (existing is null)
        {
            ticketType.CreatedAt = DateTime.UtcNow;
            ticketType.UpdatedAt = DateTime.UtcNow;
            _db.TicketTypes.Add(ticketType);
        }
        else
        {
            existing.Name = ticketType.Name;
            existing.Price = ticketType.Price;
            existing.Currency = ticketType.Currency;
            existing.Capacity = ticketType.Capacity;
            existing.QuantitySold = ticketType.QuantitySold;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
