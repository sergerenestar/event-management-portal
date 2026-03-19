using EventPortal.Api.Modules.AuditLogs.Services;
using EventPortal.Api.Modules.Events.Entities;
using EventPortal.Api.Modules.Events.Integrations;
using EventPortal.Api.Modules.Events.Repositories;
using EventPortal.Api.Modules.Registrations.Entities;
using EventPortal.Api.Modules.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventPortal.Api.Modules.Events.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepo;
    private readonly IEventbriteClient _eventbriteClient;
    private readonly IAuditLogger _auditLogger;
    private readonly AppDbContext _db;
    private readonly ILogger<EventService> _logger;
    private readonly string _organizationId;

    public EventService(
        IEventRepository eventRepo,
        IEventbriteClient eventbriteClient,
        IAuditLogger auditLogger,
        AppDbContext db,
        IConfiguration config,
        ILogger<EventService> logger)
    {
        _eventRepo = eventRepo;
        _eventbriteClient = eventbriteClient;
        _auditLogger = auditLogger;
        _db = db;
        _logger = logger;
        _organizationId = config["Eventbrite:OrganizationId"] ?? string.Empty;
    }

    public Task<List<Event>> GetEventsAsync() =>
        _eventRepo.GetAllAsync();

    public Task<Event?> GetEventByIdAsync(int id) =>
        _eventRepo.GetByIdAsync(id);

    public async Task SyncEventsAsync()
    {
        _logger.LogInformation("Starting Eventbrite event sync for organization {OrgId}", _organizationId);

        var ebEvents = await _eventbriteClient.GetEventsAsync(_organizationId);

        foreach (var ebEvent in ebEvents)
        {
            var ev = new Event
            {
                ExternalEventbriteId = ebEvent.Id,
                Name = ebEvent.Name?.Text ?? string.Empty,
                Slug = ebEvent.Slug,
                StartDate = ebEvent.Start?.Utc ?? DateTime.MinValue,
                EndDate = ebEvent.End?.Utc ?? DateTime.MinValue,
                Venue = ebEvent.Venue?.Name ?? string.Empty,
                Status = ebEvent.Status,
                ThumbnailUrl = ebEvent.Logo?.Url ?? string.Empty,
            };

            await _eventRepo.UpsertAsync(ev);

            // Re-fetch to get the Id after upsert
            var saved = await _eventRepo.GetByExternalIdAsync(ebEvent.Id);
            if (saved is null) continue;

            var ticketClasses = await _eventbriteClient.GetTicketClassesAsync(ebEvent.Id);
            foreach (var tc in ticketClasses)
            {
                var ticketType = new TicketType
                {
                    EventId = saved.Id,
                    ExternalTicketClassId = tc.Id,
                    Name = tc.Name,
                    Price = tc.Cost is not null ? tc.Cost.Value / 100m : 0m,
                    Currency = tc.Currency,
                    Capacity = tc.Capacity,
                    QuantitySold = tc.QuantitySold,
                };

                await _eventRepo.UpsertTicketTypeAsync(ticketType);
            }
        }

        await _auditLogger.LogAsync("EventSync", 0, $"Synced {ebEvents.Count} events from Eventbrite");
        _logger.LogInformation("Eventbrite event sync complete. {Count} events processed.", ebEvents.Count);
    }

    public async Task SyncEventDetailAsync(int eventId)
    {
        var ev = await _eventRepo.GetByIdAsync(eventId);
        if (ev is null)
        {
            _logger.LogWarning("SyncEventDetailAsync: event {EventId} not found", eventId);
            return;
        }

        _logger.LogInformation("Syncing registrations for event {EventId} ({ExternalId})",
            eventId, ev.ExternalEventbriteId);

        var orders = await _eventbriteClient.GetOrdersAsync(ev.ExternalEventbriteId);

        // Build a lookup of ExternalTicketClassId -> local TicketType.Id
        var ticketTypeLookup = await _db.TicketTypes
            .Where(t => t.EventId == eventId)
            .ToDictionaryAsync(t => t.ExternalTicketClassId, t => t);

        foreach (var order in orders)
        {
            foreach (var attendee in order.Attendees)
            {
                ticketTypeLookup.TryGetValue(attendee.TicketClassId, out var ticketType);

                var existing = await _db.Registrations.FirstOrDefaultAsync(r =>
                    r.ExternalAttendeeId == attendee.Id);

                if (existing is null)
                {
                    _db.Registrations.Add(new Registration
                    {
                        EventId = eventId,
                        TicketTypeId = ticketType?.Id ?? 0,
                        ExternalOrderId = order.Id,
                        ExternalAttendeeId = attendee.Id,
                        AttendeeName = attendee.Profile?.Name ?? string.Empty,
                        AttendeeEmail = attendee.Profile?.Email ?? string.Empty,
                        RegisteredAt = attendee.Created,
                        CheckInStatus = attendee.CheckedIn ? "checked_in" : "not_checked_in",
                        SourceSystem = "eventbrite",
                    });
                }
                else
                {
                    existing.CheckInStatus = attendee.CheckedIn ? "checked_in" : "not_checked_in";
                    existing.AttendeeName = attendee.Profile?.Name ?? existing.AttendeeName;
                    existing.AttendeeEmail = attendee.Profile?.Email ?? existing.AttendeeEmail;
                }
            }
        }

        await _db.SaveChangesAsync();

        // Update QuantitySold on each TicketType from live counts
        foreach (var ticketType in ticketTypeLookup.Values)
        {
            ticketType.QuantitySold = await _db.Registrations
                .CountAsync(r => r.EventId == eventId && r.TicketTypeId == ticketType.Id);
            ticketType.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("SyncEventDetailAsync complete for event {EventId}. {OrderCount} orders processed.",
            eventId, orders.Count);
    }
}
