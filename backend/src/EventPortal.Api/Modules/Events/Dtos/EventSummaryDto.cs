namespace EventPortal.Api.Modules.Events.Dtos;

public class EventSummaryDto
{
    public int Id { get; set; }
    public string ExternalEventbriteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int TotalRegistrations { get; set; }
    public int TotalCapacity { get; set; }
}
