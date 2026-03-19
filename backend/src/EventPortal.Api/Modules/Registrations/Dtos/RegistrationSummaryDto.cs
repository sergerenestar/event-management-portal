namespace EventPortal.Api.Modules.Registrations.Dtos;

public class RegistrationSummaryDto
{
    public int TotalRegistrations { get; set; }
    public int TotalCapacity { get; set; }
    public double FillRate { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
