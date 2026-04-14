namespace Eskineria.Core.Auditing.Models;

public class AuditLogIntegrity
{
    public long Id { get; set; }
    public string AuditTable { get; set; } = "AppAuditLogs";
    public long AuditLogId { get; set; }
    public string PreviousHash { get; set; } = string.Empty;
    public string CurrentHash { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "HMACSHA256";
    public string KeyId { get; set; } = "v1";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
