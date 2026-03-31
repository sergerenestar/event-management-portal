namespace EventPortal.Api.Modules.Registrations.Dtos;

public class AttendeeTypeBreakdownDto
{
    public string AttendeeType { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class AttendeeTypeBreakdownResultDto
{
    public int EventId { get; set; }
    public int TotalRegistrations { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public List<AttendeeTypeBreakdownDto> Breakdown { get; set; } = new();
}
