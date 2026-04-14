using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Auditing.Models;
using Eskineria.Core.Settings.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Auditing.Services;

public class EfAuditingPersistence : IAuditingPersistence
{
    private readonly DbContext _dbContext;

    public EfAuditingPersistence(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetSettingValueAsync(string name, CancellationToken cancellationToken)
    {
        var settings = await GetSettingValuesAsync(new[] { name }, cancellationToken);
        return settings.GetValueOrDefault(name);
    }

    public Task<IReadOnlyDictionary<string, string?>> GetSettingValuesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(names);

        if (names.Count == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string?>>(new Dictionary<string, string?>(StringComparer.Ordinal));
        }

        var requestedNames = names
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (requestedNames.Length == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string?>>(new Dictionary<string, string?>(StringComparer.Ordinal));
        }

        return LoadSettingValuesAsync(requestedNames, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, string?>> LoadSettingValuesAsync(
        string[] requestedNames,
        CancellationToken cancellationToken)
    {
        var values = await _dbContext.Set<Setting>()
            .AsNoTracking()
            .Where(x => requestedNames.Contains(x.Name))
            .Select(x => new { x.Name, x.Value })
            .ToListAsync(cancellationToken);

        return values.ToDictionary(x => x.Name, x => (string?)x.Value, StringComparer.Ordinal);
    }

    public async Task<long> InsertAppAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        _dbContext.Set<AuditLog>().Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return auditLog.Id;
    }

    public Task<string?> GetPreviousIntegrityHashAsync(string auditTable, CancellationToken cancellationToken)
    {
        return _dbContext.Set<AuditLogIntegrity>()
            .AsNoTracking()
            .Where(x => x.AuditTable == auditTable)
            .OrderByDescending(x => x.AuditLogId)
            .Select(x => x.CurrentHash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AppendIntegrityAsync(AuditLogIntegrity integrity, CancellationToken cancellationToken)
    {
        _dbContext.Set<AuditLogIntegrity>().Add(integrity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
