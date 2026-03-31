using EventPortal.Api.Modules.Registrations.Dtos;

namespace EventPortal.Api.Modules.Registrations.Services;

public interface IRegistrationService
{
    Task<RegistrationSummaryDto> GetSummaryAsync(int eventId);
    Task<List<TicketTypeSummaryDto>> GetByTicketTypeAsync(int eventId);
    Task<List<DailySnapshotDto>> GetDailyTrendsAsync(int eventId);
    Task SyncRegistrationsAsync(int eventId);
    Task AggregateSnapshotsAsync(int eventId);
    Task<bool> EventExistsAsync(int eventId);
    Task<LocationBreakdownResultDto> GetLocationBreakdownAsync(int eventId);
    Task<AttendeeTypeBreakdownResultDto> GetAttendeeTypeBreakdownAsync(int eventId);
}
