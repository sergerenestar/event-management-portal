using EventPortal.Api.Modules.AuditLogs.Services;
using EventPortal.Api.Modules.Events.Entities;
using EventPortal.Api.Modules.Events.Integrations;
using EventPortal.Api.Modules.Events.Repositories;
using EventPortal.Api.Modules.Events.Services;
using EventPortal.Api.Modules.Shared.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EventPortal.Tests.Events;

public class EventServiceTests : IDisposable
{
    // ── Mocks ─────────────────────────────────────────────────────────────
    private readonly Mock<IEventRepository>    _eventRepo       = new();
    private readonly Mock<IEventbriteClient>   _eventbriteClient = new();
    private readonly Mock<IAuditLogger>        _audit           = new();

    private readonly AppDbContext _db;
    private readonly EventService _sut;

    public EventServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Eventbrite:OrganizationId"] = "test-org-123",
            })
            .Build();

        _audit
            .Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _sut = new EventService(
            _eventRepo.Object,
            _eventbriteClient.Object,
            _audit.Object,
            _db,
            config,
            NullLogger<EventService>.Instance);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static EventbriteEvent MakeEbEvent(string id, string name) => new()
    {
        Id   = id,
        Name = new EventbriteText { Text = name },
        Start = new EventbriteDateTime { Utc = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc) },
        End   = new EventbriteDateTime { Utc = new DateTime(2026, 6, 1, 17, 0, 0, DateTimeKind.Utc) },
        Venue = new EventbriteVenue { Name = "Main Hall" },
        Status = "live",
        Slug = "test-event",
    };

    private static Event MakeSavedEvent(int id, string externalId) => new()
    {
        Id                   = id,
        ExternalEventbriteId = externalId,
        Name                 = "Saved Event",
        StartDate            = DateTime.UtcNow,
        EndDate              = DateTime.UtcNow.AddDays(1),
        CreatedAt            = DateTime.UtcNow,
        UpdatedAt            = DateTime.UtcNow,
    };

    // ── SyncEventsAsync — two events upserted ─────────────────────────────

    [Fact]
    public async Task SyncEventsAsync_TwoEventsFromEventbrite_BothUpserted()
    {
        // Arrange
        var ebEvents = new List<EventbriteEvent>
        {
            MakeEbEvent("eb-001", "Conference A"),
            MakeEbEvent("eb-002", "Conference B"),
        };

        _eventbriteClient
            .Setup(c => c.GetEventsAsync("test-org-123"))
            .ReturnsAsync(ebEvents);

        _eventbriteClient
            .Setup(c => c.GetTicketClassesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<EventbriteTicketClass>());

        _eventRepo
            .Setup(r => r.UpsertAsync(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        _eventRepo
            .Setup(r => r.GetByExternalIdAsync("eb-001"))
            .ReturnsAsync(MakeSavedEvent(1, "eb-001"));

        _eventRepo
            .Setup(r => r.GetByExternalIdAsync("eb-002"))
            .ReturnsAsync(MakeSavedEvent(2, "eb-002"));

        // Act
        await _sut.SyncEventsAsync();

        // Assert — UpsertAsync called once per event
        _eventRepo.Verify(r => r.UpsertAsync(It.IsAny<Event>()), Times.Exactly(2));

        // Assert — audit log written
        _audit.Verify(a => a.LogAsync("EventSync", 0, It.Is<string?>(s => s != null && s.Contains("2"))), Times.Once);
    }

    // ── SyncEventsAsync — existing event updated, not duplicated ──────────

    [Fact]
    public async Task SyncEventsAsync_ExistingEvent_UpdatedNotDuplicated()
    {
        // Arrange — one event in Eventbrite
        var ebEvents = new List<EventbriteEvent> { MakeEbEvent("eb-existing", "Updated Name") };

        _eventbriteClient
            .Setup(c => c.GetEventsAsync("test-org-123"))
            .ReturnsAsync(ebEvents);

        _eventbriteClient
            .Setup(c => c.GetTicketClassesAsync("eb-existing"))
            .ReturnsAsync(new List<EventbriteTicketClass>());

        _eventRepo
            .Setup(r => r.UpsertAsync(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        _eventRepo
            .Setup(r => r.GetByExternalIdAsync("eb-existing"))
            .ReturnsAsync(MakeSavedEvent(5, "eb-existing"));

        // Act
        await _sut.SyncEventsAsync();

        // Assert — UpsertAsync called exactly once (no duplication)
        _eventRepo.Verify(r => r.UpsertAsync(It.Is<Event>(e =>
            e.ExternalEventbriteId == "eb-existing" &&
            e.Name                 == "Updated Name")),
            Times.Once);
    }

    // ── GetEventsAsync — returns ordered list from repository ─────────────

    [Fact]
    public async Task GetEventsAsync_ReturnsEventsOrderedByStartDateDesc()
    {
        // Arrange — repository returns events already ordered desc (as contract requires)
        var orderedEvents = new List<Event>
        {
            new() { Id = 3, Name = "Latest",  StartDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, Name = "Middle",  StartDate = new DateTime(2026,  6, 1, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 1, Name = "Earliest",StartDate = new DateTime(2026,  1, 1, 0, 0, 0, DateTimeKind.Utc) },
        };

        _eventRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(orderedEvents);

        // Act
        var result = await _sut.GetEventsAsync();

        // Assert — ordering is preserved end-to-end
        result.Should().HaveCount(3);
        result[0].StartDate.Should().BeAfter(result[1].StartDate);
        result[1].StartDate.Should().BeAfter(result[2].StartDate);
        result[0].Name.Should().Be("Latest");
        result[2].Name.Should().Be("Earliest");
    }

    public void Dispose() => _db.Dispose();
}
