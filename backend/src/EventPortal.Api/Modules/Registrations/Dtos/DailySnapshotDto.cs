namespace EventPortal.Api.Modules.Registrations.Dtos;

public class DailySnapshotDto
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public int TicketTypeId { get; set; }
}
