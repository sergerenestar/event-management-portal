using EventPortal.Api.Modules.Events.Dtos;
using EventPortal.Api.Modules.Events.Jobs;
using EventPortal.Api.Modules.Events.Services;
using EventPortal.Api.Modules.Shared.Security;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPortal.Api.Modules.Events.Controllers;

[ApiController]
[Route("api/v1/events")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBackgroundJobClient _jobs;

    public EventsController(IEventService eventService, IBackgroundJobClient jobs)
    {
        _eventService = eventService;
        _jobs = jobs;
    }

    /// <summary>Returns all synced events ordered by start date descending.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _eventService.GetEventsAsync();

        var dtos = events.Select(e => new EventSummaryDto
        {
            Id = e.Id,
            ExternalEventbriteId = e.ExternalEventbriteId,
            Name = e.Name,
            Slug = e.Slug,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            Venue = e.Venue,
            Status = e.Status,
            ThumbnailUrl = e.ThumbnailUrl,
            TotalRegistrations = e.TicketTypes.Sum(t => t.QuantitySold),
            TotalCapacity = e.TicketTypes.Sum(t => t.Capacity),
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>Returns full event detail including ticket types.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _eventService.GetEventByIdAsync(id);
        if (ev is null)
            return NotFound(new { message = $"Event {id} not found." });

        var dto = new EventDetailDto
        {
            Id = ev.Id,
            ExternalEventbriteId = ev.ExternalEventbriteId,
            Name = ev.Name,
            Slug = ev.Slug,
            StartDate = ev.StartDate,
            EndDate = ev.EndDate,
            Venue = ev.Venue,
            Status = ev.Status,
            ThumbnailUrl = ev.ThumbnailUrl,
            CreatedAt = ev.CreatedAt,
            UpdatedAt = ev.UpdatedAt,
            TicketTypes = ev.TicketTypes.Select(t => new EventTicketTypeDto
            {
                Id = t.Id,
                ExternalTicketClassId = t.ExternalTicketClassId,
                Name = t.Name,
                Price = t.Price,
                Currency = t.Currency,
                Capacity = t.Capacity,
                QuantitySold = t.QuantitySold,
            }).ToList(),
        };

        return Ok(dto);
    }

    /// <summary>Enqueues an Eventbrite event sync job.</summary>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult SyncEvents()
    {
        _jobs.Enqueue<EventSyncJob>(j => j.ExecuteAsync());
        return Accepted(new { message = "Event sync job enqueued." });
    }
}
