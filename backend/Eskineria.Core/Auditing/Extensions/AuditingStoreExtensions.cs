using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Auditing.Models;

namespace Eskineria.Core.Auditing.Extensions;

public static class AuditingStoreExtensions
{
    public static Task SaveAsync(
        this IAuditingStore auditingStore,
        string serviceName,
        string methodName,
        string? parameters = null,
        int executionDurationMs = 0,
        Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(auditingStore);

        return auditingStore.SaveAsync(new AuditLog
        {
            ServiceName = serviceName,
            MethodName = methodName,
            Parameters = parameters ?? string.Empty,
            ExecutionDuration = executionDurationMs,
            Exception = exception?.ToString()
        });
    }
}
