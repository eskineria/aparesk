using System;

namespace Eskineria.Core.Auditing.Responses;

public class AuditLogListItemDto
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public int ExecutionDuration { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? BrowserInfo { get; set; }
    public string? Exception { get; set; }
}
