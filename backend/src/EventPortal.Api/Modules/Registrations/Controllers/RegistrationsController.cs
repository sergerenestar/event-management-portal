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
}
