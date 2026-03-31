using EventPortal.Api.Modules.Events.Services;
using EventPortal.Api.Modules.Registrations.Dtos;
using EventPortal.Api.Modules.Registrations.Entities;
using EventPortal.Api.Modules.Shared.Persistence;
using EventPortal.Api.Modules.Shared.Utilities;
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

    public async Task<bool> EventExistsAsync(int eventId)
        => await _db.Events.AnyAsync(e => e.Id == eventId);

    public async Task<LocationBreakdownResultDto> GetLocationBreakdownAsync(int eventId)
    {
        _logger.LogInformation("LocationBreakdown queried for event {EventId}", eventId);

        var ticketTypes = await _db.TicketTypes
            .Where(t => t.EventId == eventId)
            .ToListAsync();

        var ticketTypeMap = ticketTypes.ToDictionary(t => t.Id, t => t.Name);

        var groups = await _db.Registrations
            .Where(r => r.EventId == eventId)
            .GroupBy(r => r.TicketTypeId)
            .Select(g => new { TicketTypeId = g.Key, Count = g.Count() })
            .ToListAsync();

        int totalRegistrations = groups.Sum(g => g.Count);

        DateTime? lastSyncedAt = totalRegistrations > 0
            ? await _db.Registrations
                .Where(r => r.EventId == eventId)
                .MaxAsync(r => (DateTime?)r.RegisteredAt)
            : null;

        var locationGroups = groups
            .GroupBy(g => ticketTypeMap.TryGetValue(g.TicketTypeId, out var name)
                ? TicketTypeNameParser.ParseLocation(name)
                : "Unknown")
            .Select(g => new { Location = g.Key, Count = g.Sum(x => x.Count) })
            .OrderByDescending(x => x.Count)
            .ToList();

        var result = new List<LocationBreakdownDto>();
        int otherCount = 0;
        int rank = 0;

        foreach (var loc in locationGroups)
        {
            rank++;
            if (rank <= 10)
            {
                result.Add(new LocationBreakdownDto
                {
                    Location = loc.Location,
                    Count = loc.Count,
                    Percentage = totalRegistrations > 0
                        ? Math.Round(loc.Count * 100m / totalRegistrations, 1)
                        : 0,
                });
            }
            else
            {
                otherCount += loc.Count;
            }
        }

        if (otherCount > 0)
        {
            result.Add(new LocationBreakdownDto
            {
                Location = "Other",
                Count = otherCount,
                Percentage = totalRegistrations > 0
                    ? Math.Round(otherCount * 100m / totalRegistrations, 1)
                    : 0,
            });
        }

        _logger.LogInformation("LocationBreakdown for event {EventId}: {LocationCount} locations, {Total} total registrations",
            eventId, result.Count, totalRegistrations);

        return new LocationBreakdownResultDto
        {
            EventId = eventId,
            TotalRegistrations = totalRegistrations,
            LastSyncedAt = lastSyncedAt,
            Locations = result,
        };
    }

    public async Task<AttendeeTypeBreakdownResultDto> GetAttendeeTypeBreakdownAsync(int eventId)
    {
        _logger.LogInformation("AttendeeTypeBreakdown queried for event {EventId}", eventId);

        var ticketTypes = await _db.TicketTypes
            .Where(t => t.EventId == eventId)
            .ToListAsync();

        var ticketTypeMap = ticketTypes.ToDictionary(t => t.Id, t => t.Name);

        var groups = await _db.Registrations
            .Where(r => r.EventId == eventId)
            .GroupBy(r => r.TicketTypeId)
            .Select(g => new { TicketTypeId = g.Key, Count = g.Count() })
            .ToListAsync();

        int totalRegistrations = groups.Sum(g => g.Count);

        DateTime? lastSyncedAt = totalRegistrations > 0
            ? await _db.Registrations
                .Where(r => r.EventId == eventId)
                .MaxAsync(r => (DateTime?)r.RegisteredAt)
            : null;

        var typeGroups = groups
            .GroupBy(g => ticketTypeMap.TryGetValue(g.TicketTypeId, out var name)
                ? TicketTypeNameParser.ParseAttendeeType(name)
                : "Other")
            .Select(g => new { AttendeeType = g.Key, Count = g.Sum(x => x.Count) })
            .ToList();

        var sortOrder = new Dictionary<string, int> { ["Adult"] = 0, ["Children"] = 1, ["Other"] = 2 };
        typeGroups = typeGroups.OrderBy(g => sortOrder.GetValueOrDefault(g.AttendeeType, 3)).ToList();

        var breakdown = typeGroups
            .Where(g => g.Count > 0)
            .Select(g => new AttendeeTypeBreakdownDto
            {
                AttendeeType = g.AttendeeType,
                Count = g.Count,
                Percentage = totalRegistrations > 0
                    ? Math.Round(g.Count * 100m / totalRegistrations, 1)
                    : 0,
            })
            .ToList();

        _logger.LogInformation("AttendeeTypeBreakdown for event {EventId}: Adult={Adult}, Children={Children}, Other={Other}",
            eventId,
            breakdown.FirstOrDefault(b => b.AttendeeType == "Adult")?.Count ?? 0,
            breakdown.FirstOrDefault(b => b.AttendeeType == "Children")?.Count ?? 0,
            breakdown.FirstOrDefault(b => b.AttendeeType == "Other")?.Count ?? 0);

        return new AttendeeTypeBreakdownResultDto
        {
            EventId = eventId,
            TotalRegistrations = totalRegistrations,
            LastSyncedAt = lastSyncedAt,
            Breakdown = breakdown,
        };
    }
}
