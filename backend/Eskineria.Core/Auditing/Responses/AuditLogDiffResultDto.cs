using System.Collections.Generic;

namespace Eskineria.Core.Auditing.Responses;

public class AuditLogDiffResultDto
{
    public long LogId { get; set; }
    public long? ComparedLogId { get; set; }
    public string Source { get; set; } = "none";
    public bool HasComparableData { get; set; }
    public List<AuditFieldDiffDto> Changes { get; set; } = new();
}
