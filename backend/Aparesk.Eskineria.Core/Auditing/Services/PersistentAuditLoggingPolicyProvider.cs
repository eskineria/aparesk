using Aparesk.Eskineria.Core.Auditing.Abstractions;
using Aparesk.Eskineria.Core.Auditing.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Aparesk.Eskineria.Core.Auditing.Services;

public sealed class PersistentAuditLoggingPolicyProvider
    : IAuditLoggingPolicyProvider, IAuditLoggingPolicyCacheInvalidator
{
    private const string CacheKey = "audit-logging-policy";
    private const string ReadOperationsSetting = "System.Audit.LogReadOperationsEnabled";
    private const string CreateOperationsSetting = "System.Audit.LogCreateOperationsEnabled";
    private const string UpdateOperationsSetting = "System.Audit.LogUpdateOperationsEnabled";
    private const string DeleteOperationsSetting = "System.Audit.LogDeleteOperationsEnabled";
    private const string OtherOperationsSetting = "System.Audit.LogOtherOperationsEnabled";
    private const string ErrorEventsSetting = "System.Audit.LogErrorEventsEnabled";

    private static readonly AuditLoggingPolicy DefaultPolicy = new();

    private readonly IAuditingPersistence _auditingPersistence;
    private readonly IMemoryCache _memoryCache;

    public PersistentAuditLoggingPolicyProvider(
        IAuditingPersistence auditingPersistence,
        IMemoryCache memoryCache)
    {
        _auditingPersistence = auditingPersistence;
        _memoryCache = memoryCache;
    }

    public Task<AuditLoggingPolicy> GetCurrentPolicyAsync(CancellationToken cancellationToken = default)
    {
        return _memoryCache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);

                var settingNames = new[]
                {
                    ReadOperationsSetting,
                    CreateOperationsSetting,
                    UpdateOperationsSetting,
                    DeleteOperationsSetting,
                    OtherOperationsSetting,
                    ErrorEventsSetting,
                };

                var settings = await _auditingPersistence.GetSettingValuesAsync(settingNames, cancellationToken);

                return new AuditLoggingPolicy
                {
                    LogReadOperations = ParseBoolean(
                        settings.GetValueOrDefault(ReadOperationsSetting),
                        DefaultPolicy.LogReadOperations),
                    LogCreateOperations = ParseBoolean(
                        settings.GetValueOrDefault(CreateOperationsSetting),
                        DefaultPolicy.LogCreateOperations),
                    LogUpdateOperations = ParseBoolean(
                        settings.GetValueOrDefault(UpdateOperationsSetting),
                        DefaultPolicy.LogUpdateOperations),
                    LogDeleteOperations = ParseBoolean(
                        settings.GetValueOrDefault(DeleteOperationsSetting),
                        DefaultPolicy.LogDeleteOperations),
                    LogOtherOperations = ParseBoolean(
                        settings.GetValueOrDefault(OtherOperationsSetting),
                        DefaultPolicy.LogOtherOperations),
                    LogErrorEvents = ParseBoolean(
                        settings.GetValueOrDefault(ErrorEventsSetting),
                        DefaultPolicy.LogErrorEvents),
                };
            })!;
    }

    public void Invalidate()
    {
        _memoryCache.Remove(CacheKey);
    }

    private static bool ParseBoolean(string? value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
