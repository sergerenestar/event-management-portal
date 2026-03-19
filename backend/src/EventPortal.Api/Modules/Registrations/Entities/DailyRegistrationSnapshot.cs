using EventPortal.Api.Modules.Events.Entities;

namespace EventPortal.Api.Modules.Registrations.Entities;

public class DailyRegistrationSnapshot
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int TicketTypeId { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public int RegistrationCount { get; set; }

    public Event Event { get; set; } = null!;
    public TicketType TicketType { get; set; } = null!;
}
