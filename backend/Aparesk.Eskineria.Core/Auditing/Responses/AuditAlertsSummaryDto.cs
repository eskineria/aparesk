using System;
using System.Collections.Generic;

namespace Aparesk.Eskineria.Core.Auditing.Responses;

public class AuditAlertsSummaryDto
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public List<AuditAlertDto> Alerts { get; set; } = new();
}
