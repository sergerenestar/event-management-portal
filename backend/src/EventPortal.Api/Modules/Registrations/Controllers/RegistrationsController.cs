using EventPortal.Api.Modules.Registrations.Dtos;
using EventPortal.Api.Modules.Registrations.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventPortal.Api.Modules.Registrations.Controllers;

[ApiController]
[Route("api/v1/events/{eventId:int}/registrations")]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IBackgroundJobClient _jobs;

    public RegistrationsController(IRegistrationService registrationService, IBackgroundJobClient jobs)
    {
        _registrationService = registrationService;
        _jobs = jobs;
    }

    /// <summary>Returns aggregated registration totals and fill rate for an event.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(RegistrationSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(int eventId)
    {
        var summary = await _registrationService.GetSummaryAsync(eventId);
        return Ok(summary);
    }

    /// <summary>Returns registration count and fill percentage broken down by ticket type.</summary>
    [HttpGet("by-ticket-type")]
    [ProducesResponseType(typeof(List<TicketTypeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTicketType(int eventId)
    {
        var breakdown = await _registrationService.GetByTicketTypeAsync(eventId);
        return Ok(breakdown);
    }

    /// <summary>Returns daily registration snapshot data for trend charts.</summary>
    [HttpGet("daily-trends")]
    [ProducesResponseType(typeof(List<DailySnapshotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyTrends(int eventId)
    {
        var trends = await _registrationService.GetDailyTrendsAsync(eventId);
        return Ok(trends);
    }

    /// <summary>Enqueues a registration sync job for the specified event.</summary>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult SyncRegistrations(int eventId)
    {
        _jobs.Enqueue<IRegistrationService>(s => s.SyncRegistrationsAsync(eventId));
        return Accepted(new { message = $"Registration sync job enqueued for event {eventId}." });
    }

    /// <summary>Returns registration counts grouped by location parsed from ticket type names.</summary>
    [HttpGet("location-breakdown")]
    [ProducesResponseType(typeof(LocationBreakdownResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationBreakdown(int eventId)
    {
        var eventExists = await _registrationService.EventExistsAsync(eventId);
        if (!eventExists) return NotFound(new { title = "Event not found", status = 404, detail = $"No event with ID {eventId} exists." });
        var result = await _registrationService.GetLocationBreakdownAsync(eventId);
        return Ok(result);
    }

    /// <summary>Returns registration counts split by attendee type (Adult/Children/Other) from ticket type names.</summary>
    [HttpGet("attendee-type-breakdown")]
    [ProducesResponseType(typeof(AttendeeTypeBreakdownResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttendeeTypeBreakdown(int eventId)
    {
        var eventExists = await _registrationService.EventExistsAsync(eventId);
        if (!eventExists) return NotFound(new { title = "Event not found", status = 404, detail = $"No event with ID {eventId} exists." });
        var result = await _registrationService.GetAttendeeTypeBreakdownAsync(eventId);
        return Ok(result);
    }
}
