namespace Aparesk.Eskineria.Core.Auditing.Responses;

public class AuditAlertDto
{
    public string Key { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
    public int MetricValue { get; set; }
}
