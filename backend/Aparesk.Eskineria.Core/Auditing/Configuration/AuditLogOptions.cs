namespace Aparesk.Eskineria.Core.Auditing.Configuration;

public class AuditLogOptions
{
    public int IntegritySampleSize { get; set; } = 10;
    public int ErrorAlertThreshold { get; set; } = 10;
    public int DeleteAlertThreshold { get; set; } = 30;
    public int RoleSwitchAlertThreshold { get; set; } = 10;

    public string AppAuditLogsTableName { get; set; } = "[dbo].[AppAuditLogs]";
    public string AuditDiffViewerFeatureFlagKey { get; set; } = "Compliance.AuditDiffViewer";
    public string AuditHardeningFeatureFlagKey { get; set; } = "Compliance.AuditLogHardening";
    public string IntegrityTableName { get; set; } = "[dbo].[AppAuditLogIntegrities]";
    public string DiffSourceNone { get; set; } = "none";
    public string DiffSourceDisabled { get; set; } = "disabled";
    public string DiffSourcePayload { get; set; } = "payload";
    public string DiffSourcePreviousLog { get; set; } = "previous_log";
    public string DiffSourceSnapshot { get; set; } = "snapshot";
}
