namespace EventPortal.Api.Modules.Registrations.Dtos;

public class TicketTypeSummaryDto
{
    public int TicketTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public int Capacity { get; set; }
    public double FillPercentage { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}
