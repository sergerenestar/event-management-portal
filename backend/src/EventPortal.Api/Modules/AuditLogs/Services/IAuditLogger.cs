namespace EventPortal.Api.Modules.AuditLogs.Services;

public interface IAuditLogger
{
    /// <summary>
    /// Records an audit event. Implementations may write to a database, structured log,
    /// or both. Never throws — failures are swallowed and logged internally.
    /// </summary>
    Task LogAsync(string eventType, int adminUserId, string? details = null);
}
