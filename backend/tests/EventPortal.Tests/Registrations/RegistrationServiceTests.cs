using EventPortal.Api.Modules.Events.Entities;
using EventPortal.Api.Modules.Events.Services;
using EventPortal.Api.Modules.Registrations.Entities;
using EventPortal.Api.Modules.Registrations.Services;
using EventPortal.Api.Modules.Shared.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EventPortal.Tests.Registrations;

public class RegistrationServiceTests : IDisposable
{
    private readonly AppDbContext        _db;
    private readonly Mock<IEventService> _eventService = new();
    private readonly RegistrationService _sut;

    // Fixed event/ticket type IDs used across tests
    private const int EventId       = 10;
    private const int TicketTypeIdA = 1;
    private const int TicketTypeIdB = 2;

    public RegistrationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _sut = new RegistrationService(
            _db,
            _eventService.Object,
            NullLogger<RegistrationService>.Instance);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task SeedEventWithTicketTypesAsync(
        int soldA = 0, int capacityA = 100,
        int soldB = 0, int capacityB = 50)
    {
        var ev = new Event
        {
            Id                   = EventId,
            ExternalEventbriteId = "eb-test",
            Name                 = "Test Event",
            StartDate            = DateTime.UtcNow,
            EndDate              = DateTime.UtcNow.AddDays(1),
            Status               = "live",
            CreatedAt            = DateTime.UtcNow,
            UpdatedAt            = DateTime.UtcNow,
        };
        _db.Events.Add(ev);

        _db.TicketTypes.AddRange(
            new TicketType
            {
                Id                   = TicketTypeIdA,
                EventId              = EventId,
                ExternalTicketClassId = "tc-a",
                Name                 = "General Admission",
                Price                = 25.00m,
                Currency             = "USD",
                Capacity             = capacityA,
                QuantitySold         = soldA,
                CreatedAt            = DateTime.UtcNow,
                UpdatedAt            = DateTime.UtcNow,
            },
            new TicketType
            {
                Id                   = TicketTypeIdB,
                EventId              = EventId,
                ExternalTicketClassId = "tc-b",
                Name                 = "VIP",
                Price                = 75.00m,
                Currency             = "USD",
                Capacity             = capacityB,
                QuantitySold         = soldB,
                CreatedAt            = DateTime.UtcNow,
                UpdatedAt            = DateTime.UtcNow,
            });

        await _db.SaveChangesAsync();
    }

    private async Task SeedRegistrationsAsync(IEnumerable<(DateTime registeredAt, int ticketTypeId)> entries)
    {
        int i = 1;
        foreach (var (registeredAt, ticketTypeId) in entries)
        {
            _db.Registrations.Add(new Registration
            {
                Id                 = i++,
                EventId            = EventId,
                TicketTypeId       = ticketTypeId,
                ExternalOrderId    = $"order-{i}",
                ExternalAttendeeId = $"attendee-{i}",
                RegisteredAt       = registeredAt,
                CheckInStatus      = "not_checked_in",
                SourceSystem       = "eventbrite",
            });
        }
        await _db.SaveChangesAsync();
    }

    // ── GetSummaryAsync — fill rate calculation ───────────────────────────

    [Fact]
    public async Task GetSummaryAsync_CorrectFillRateCalculation()
    {
        // Arrange: 75 sold out of 150 total capacity = 50% fill rate
        await SeedEventWithTicketTypesAsync(soldA: 50, capacityA: 100, soldB: 25, capacityB: 50);

        // Act
        var summary = await _sut.GetSummaryAsync(EventId);

        // Assert
        summary.TotalRegistrations.Should().Be(75);
        summary.TotalCapacity.Should().Be(150);
        summary.FillRate.Should().Be(50.0);
        summary.LastSyncAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummaryAsync_ZeroCapacity_FillRateIsZeroNotDivisionError()
    {
        // Arrange
        await SeedEventWithTicketTypesAsync(soldA: 0, capacityA: 0, soldB: 0, capacityB: 0);

        // Act
        var summary = await _sut.GetSummaryAsync(EventId);

        // Assert — no DivideByZeroException, FillRate is 0
        summary.FillRate.Should().Be(0);
    }

    // ── GetDailyTrendsAsync — ordered by date asc ─────────────────────────

    [Fact]
    public async Task GetDailyTrendsAsync_ReturnsSnapshotsOrderedByDateAsc()
    {
        // Arrange — seed snapshots deliberately out of order
        await SeedEventWithTicketTypesAsync();

        _db.DailyRegistrationSnapshots.AddRange(
            new DailyRegistrationSnapshot
            {
                EventId           = EventId,
                TicketTypeId      = TicketTypeIdA,
                SnapshotDate      = new DateOnly(2026, 6, 3),
                RegistrationCount = 5,
            },
            new DailyRegistrationSnapshot
            {
                EventId           = EventId,
                TicketTypeId      = TicketTypeIdA,
                SnapshotDate      = new DateOnly(2026, 6, 1),
                RegistrationCount = 10,
            },
            new DailyRegistrationSnapshot
            {
                EventId           = EventId,
                TicketTypeId      = TicketTypeIdA,
                SnapshotDate      = new DateOnly(2026, 6, 2),
                RegistrationCount = 8,
            });
        await _db.SaveChangesAsync();

        // Act
        var trends = await _sut.GetDailyTrendsAsync(EventId);

        // Assert — ascending date order
        trends.Should().HaveCount(3);
        trends[0].Date.Should().Be(new DateOnly(2026, 6, 1));
        trends[1].Date.Should().Be(new DateOnly(2026, 6, 2));
        trends[2].Date.Should().Be(new DateOnly(2026, 6, 3));
        trends[0].Count.Should().Be(10);
        trends[1].Count.Should().Be(8);
        trends[2].Count.Should().Be(5);
    }

    // ── AggregateSnapshotsAsync — idempotent ─────────────────────────────

    [Fact]
    public async Task AggregateSnapshotsAsync_RunTwice_ProducesSameResult()
    {
        // Arrange — 3 registrations on June 1, 2 on June 2, all for ticket type A
        await SeedEventWithTicketTypesAsync();

        var baseDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        await SeedRegistrationsAsync(new[]
        {
            (baseDate.AddHours(0), TicketTypeIdA),
            (baseDate.AddHours(1), TicketTypeIdA),
            (baseDate.AddHours(2), TicketTypeIdA),
            (baseDate.AddDays(1),  TicketTypeIdA),
            (baseDate.AddDays(1).AddHours(1), TicketTypeIdA),
        });

        // Act — run twice
        await _sut.AggregateSnapshotsAsync(EventId);
        await _sut.AggregateSnapshotsAsync(EventId);

        // Assert — idempotent: same count regardless of how many times we run
        var snapshots = await _db.DailyRegistrationSnapshots
            .Where(s => s.EventId == EventId)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync();

        snapshots.Should().HaveCount(2);
        snapshots[0].SnapshotDate.Should().Be(new DateOnly(2026, 6, 1));
        snapshots[0].RegistrationCount.Should().Be(3);
        snapshots[1].SnapshotDate.Should().Be(new DateOnly(2026, 6, 2));
        snapshots[1].RegistrationCount.Should().Be(2);
    }

    [Fact]
    public async Task AggregateSnapshotsAsync_MultipleTicketTypes_CreatesSeperateBuckets()
    {
        // Arrange — registrations spread across 2 ticket types on same day
        await SeedEventWithTicketTypesAsync();

        var day = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        await SeedRegistrationsAsync(new[]
        {
            (day, TicketTypeIdA),
            (day, TicketTypeIdA),
            (day, TicketTypeIdB),
        });

        // Act
        await _sut.AggregateSnapshotsAsync(EventId);

        // Assert — one bucket per ticket type per day
        var snapshots = await _db.DailyRegistrationSnapshots
            .Where(s => s.EventId == EventId)
            .ToListAsync();

        snapshots.Should().HaveCount(2);
        snapshots.Should().ContainSingle(s => s.TicketTypeId == TicketTypeIdA && s.RegistrationCount == 2);
        snapshots.Should().ContainSingle(s => s.TicketTypeId == TicketTypeIdB && s.RegistrationCount == 1);
    }

    public void Dispose() => _db.Dispose();
}
