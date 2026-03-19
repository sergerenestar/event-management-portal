using EventPortal.Api.Modules.Registrations.Entities;
using EventPortal.Api.Modules.Shared.Persistence;

namespace EventPortal.Api.Modules.Events.Entities;

public class Event : BaseEntity
{
    public string ExternalEventbriteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;

    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
