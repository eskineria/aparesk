namespace Aparesk.Eskineria.Core.Auditing.Models;

public class AuditLog
{
    /// <summary>
    /// Database identity key.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Optional user id resolved from current principal.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Logical service/class name where the event occurred.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Method or operation name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Serialized parameters or summary payload.
    /// </summary>
    public string Parameters { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the audited operation.
    /// </summary>
    public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Operation duration in milliseconds.
    /// </summary>
    public int ExecutionDuration { get; set; }

    /// <summary>
    /// Client IP address (direct or forwarded).
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Browser/client user agent.
    /// </summary>
    public string? BrowserInfo { get; set; }

    /// <summary>
    /// Optional exception details.
    /// </summary>
    public string? Exception { get; set; }
}
