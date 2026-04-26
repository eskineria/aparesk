using Aparesk.Eskineria.Core.Auditing.Models;

namespace Aparesk.Eskineria.Core.Auditing.Abstractions;

public interface IAuditLoggingPolicyProvider
{
    Task<AuditLoggingPolicy> GetCurrentPolicyAsync(CancellationToken cancellationToken = default);
}
