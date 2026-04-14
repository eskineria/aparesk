using Eskineria.Core.Auditing.Models;

namespace Eskineria.Core.Auditing.Abstractions;

public interface IAuditLoggingPolicyProvider
{
    Task<AuditLoggingPolicy> GetCurrentPolicyAsync(CancellationToken cancellationToken = default);
}
