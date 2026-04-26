using Aparesk.Eskineria.Core.Auditing.Models;

namespace Aparesk.Eskineria.Core.Auditing.Abstractions;

public interface IAuditingStore
{
    Task SaveAsync(AuditLog auditLog);
}
