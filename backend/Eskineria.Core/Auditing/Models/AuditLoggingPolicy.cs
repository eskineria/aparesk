namespace Eskineria.Core.Auditing.Models;

public sealed class AuditLoggingPolicy
{
    public bool LogReadOperations { get; set; } = true;
    public bool LogCreateOperations { get; set; } = true;
    public bool LogUpdateOperations { get; set; } = true;
    public bool LogDeleteOperations { get; set; } = true;
    public bool LogOtherOperations { get; set; } = true;
    public bool LogErrorEvents { get; set; } = true;
}
