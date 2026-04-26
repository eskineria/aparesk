using System;
using System.Collections.Generic;

namespace Aparesk.Eskineria.Core.Auditing.Responses;

public class AuditIntegritySummaryDto
{
    public bool FeatureEnabled { get; set; }
    public bool IntegrityTableExists { get; set; }
    public int TotalAuditLogCount { get; set; }
    public int HardenedLogCount { get; set; }
    public int MissingHardeningCount { get; set; }
    public int BrokenChainCount { get; set; }
    public long? LastHardenedAuditLogId { get; set; }
    public DateTime LastVerifiedAtUtc { get; set; } = DateTime.UtcNow;
    public List<long> BrokenSampleAuditLogIds { get; set; } = new();
    public List<long> MissingSampleAuditLogIds { get; set; } = new();
}
