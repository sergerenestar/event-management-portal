namespace EventPortal.Api.Modules.AuditLogs.Services;

/// <summary>
/// Sprint 1 stub: writes structured audit events to the application log.
/// A future sprint will persist these to an AuditLogs database table.
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    public Task LogAsync(string eventType, int adminUserId, string? details = null)
    {
        _logger.LogInformation(
            "AUDIT | Event={EventType} AdminUserId={AdminUserId} Details={Details}",
            eventType, adminUserId, details ?? string.Empty);

        return Task.CompletedTask;
    }
}
