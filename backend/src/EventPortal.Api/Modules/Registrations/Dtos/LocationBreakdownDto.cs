namespace EventPortal.Api.Modules.Registrations.Dtos;

public class LocationBreakdownDto
{
    public string Location { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class LocationBreakdownResultDto
{
    public int EventId { get; set; }
    public int TotalRegistrations { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public List<LocationBreakdownDto> Locations { get; set; } = new();
}
