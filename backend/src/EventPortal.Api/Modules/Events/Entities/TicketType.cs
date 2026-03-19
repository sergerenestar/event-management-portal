using EventPortal.Api.Modules.Registrations.Entities;
using EventPortal.Api.Modules.Shared.Persistence;

namespace EventPortal.Api.Modules.Events.Entities;

public class TicketType : BaseEntity
{
    public int EventId { get; set; }
    public string ExternalTicketClassId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int QuantitySold { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
