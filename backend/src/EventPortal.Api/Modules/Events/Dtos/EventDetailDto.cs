namespace EventPortal.Api.Modules.Events.Dtos;

public class EventDetailDto
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<EventTicketTypeDto> TicketTypes { get; set; } = new();
}

public class EventTicketTypeDto
{
    public int Id { get; set; }
    public string ExternalTicketClassId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int QuantitySold { get; set; }
}
