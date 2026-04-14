namespace Eskineria.Core.Auditing.Models;

public readonly record struct AuditLogClassification(
    AuditOperationKind OperationKind,
    bool IsError);
