using EventPortal.Api.Modules.Events.Entities;

namespace EventPortal.Api.Modules.Registrations.Entities;

public class Registration
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int TicketTypeId { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public string ExternalAttendeeId { get; set; } = string.Empty;
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string CheckInStatus { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;

    public Event Event { get; set; } = null!;
    public TicketType TicketType { get; set; } = null!;
}
