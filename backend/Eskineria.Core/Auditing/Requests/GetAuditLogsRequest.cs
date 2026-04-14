using System;

namespace Eskineria.Core.Auditing.Requests;

public class GetAuditLogsRequest
{
    public long? Id { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }
    public string? UserId { get; set; }
    public bool OnlyErrors { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
