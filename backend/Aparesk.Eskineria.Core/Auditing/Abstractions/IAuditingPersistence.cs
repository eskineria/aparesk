using Aparesk.Eskineria.Core.Auditing.Models;

namespace Aparesk.Eskineria.Core.Auditing.Abstractions;

public interface IAuditingPersistence
{
    Task<string?> GetSettingValueAsync(string name, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string?>> GetSettingValuesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken);
    Task<long> InsertAppAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task<string?> GetPreviousIntegrityHashAsync(string auditTable, CancellationToken cancellationToken);
    Task AppendIntegrityAsync(AuditLogIntegrity integrity, CancellationToken cancellationToken);
}
