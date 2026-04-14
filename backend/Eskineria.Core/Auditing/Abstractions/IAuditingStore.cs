using Eskineria.Core.Auditing.Models;

namespace Eskineria.Core.Auditing.Abstractions;

public interface IAuditingStore
{
    Task SaveAsync(AuditLog auditLog);
}
