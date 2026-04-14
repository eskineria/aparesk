using System.Collections.Generic;

namespace Eskineria.Core.Auditing.Responses;

public class AuditLogFilterOptionsDto
{
    public List<string> Services { get; set; } = new();
    public List<string> Methods { get; set; } = new();
}
