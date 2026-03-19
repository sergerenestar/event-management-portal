using EventPortal.Api.Modules.Events.Services;
using EventPortal.Api.Modules.Registrations.Dtos;
using EventPortal.Api.Modules.Registrations.Entities;
using EventPortal.Api.Modules.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventPortal.Api.Modules.Registrations.Services;

public class RegistrationService : IRegistrationService
{
    private readonly AppDbContext _db;
    private readonly IEventService _eventService;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(AppDbContext db, IEventService eventService, ILogger<RegistrationService> logger)
    {
        _db = db;
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<RegistrationSummaryDto> GetSummaryAsync(int eventId)
    {
        var ticketTypes = await _db.TicketTypes
            .Where(t => t.EventId == eventId)
            .ToListAsync();

        var totalSold = ticketTypes.Sum(t => t.QuantitySold);
        var totalCapacity = ticketTypes.Sum(t => t.Capacity);
        var lastSyncAt = ticketTypes.Count > 0
            ? ticketTypes.Max(t => t.UpdatedAt)
            : (DateTime?)null;

        return new RegistrationSummaryDto
        {
            TotalRegistrations = totalSold,
            TotalCapacity = totalCapacity,
            FillRate = totalCapacity > 0 ? Math.Round((double)totalSold / totalCapacity * 100, 2) : 0,
            LastSyncAt = lastSyncAt,
        };
    }

    public async Task<List<TicketTypeSummaryDto>> GetByTicketTypeAsync(int eventId)
    {
        var ticketTypes = await _db.TicketTypes
            .Where(t => t.EventId == eventId)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return ticketTypes.Select(t => new TicketTypeSummaryDto
        {
            TicketTypeId = t.Id,
            Name = t.Name,
            QuantitySold = t.QuantitySold,
            Capacity = t.Capacity,
            FillPercentage = t.Capacity > 0 ? Math.Round((double)t.QuantitySold / t.Capacity * 100, 2) : 0,
            Price = t.Price,
            Currency = t.Currency,
        }).ToList();
    }

    public async Task<List<DailySnapshotDto>> GetDailyTrendsAsync(int eventId)
    {
        return await _db.DailyRegistrationSnapshots
            .Where(s => s.EventId == eventId)
            .Include(s => s.TicketType)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new DailySnapshotDto
            {
                Date = s.SnapshotDate,
                Count = s.RegistrationCount,
                TicketTypeName = s.TicketType.Name,
                TicketTypeId = s.TicketTypeId,
            })
            .ToListAsync();
    }

    public Task SyncRegistrationsAsync(int eventId) =>
        _eventService.SyncEventDetailAsync(eventId);

    public async Task AggregateSnapshotsAsync(int eventId)
    {
        _logger.LogInformation("Aggregating registration snapshots for event {EventId}", eventId);

        // Group registrations by (date, ticketTypeId)
        var groups = await _db.Registrations
            .Where(r => r.EventId == eventId)
            .GroupBy(r => new { Date = DateOnly.FromDateTime(r.RegisteredAt), r.TicketTypeId })
            .Select(g => new
            {
                g.Key.Date,
                g.Key.TicketTypeId,
                Count = g.Count(),
            })
            .ToListAsync();

        foreach (var group in groups)
        {
            var existing = await _db.DailyRegistrationSnapshots.FirstOrDefaultAsync(s =>
                s.EventId == eventId &&
                s.TicketTypeId == group.TicketTypeId &&
                s.SnapshotDate == group.Date);

            if (existing is null)
            {
                _db.DailyRegistrationSnapshots.Add(new DailyRegistrationSnapshot
                {
                    EventId = eventId,
                    TicketTypeId = group.TicketTypeId,
                    SnapshotDate = group.Date,
                    RegistrationCount = group.Count,
                });
            }
            else
            {
                existing.RegistrationCount = group.Count;
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Snapshot aggregation complete for event {EventId}. {Count} date/ticket buckets processed.",
            eventId, groups.Count);
    }
}
