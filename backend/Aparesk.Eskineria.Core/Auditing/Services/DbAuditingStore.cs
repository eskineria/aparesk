using Aparesk.Eskineria.Core.Auditing.Abstractions;
using Aparesk.Eskineria.Core.Auditing.Models;
using Aparesk.Eskineria.Core.Auditing.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Aparesk.Eskineria.Core.Auditing.Services;

public class DbAuditingStore : IAuditingStore
{
    private const string AppAuditLogsTableName = "AppAuditLogs";
    private const string IntegrityAlgorithm = "HMACSHA256";
    private const int ServiceNameMaxLength = 200;
    private const int MethodNameMaxLength = 200;
    private const int ParametersMaxLength = 2000;
    private const int ClientIpAddressMaxLength = 50;
    private const int BrowserInfoMaxLength = 500;
    private const int MinimumHardeningSecretLength = 32;
    private static readonly SemaphoreSlim AuditIntegrityWriteLock = new(1, 1);

    private readonly IAuditingPersistence _auditingPersistence;
    private readonly IAuditLoggingPolicyProvider _auditLoggingPolicyProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DbAuditingStore> _logger;
    private readonly string _hardeningSecret;
    private readonly string _hardeningKeyId;
    private readonly bool _isHardeningEnabled;
    public DbAuditingStore(
        IAuditingPersistence auditingPersistence,
        IAuditLoggingPolicyProvider auditLoggingPolicyProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DbAuditingStore> logger,
        IConfiguration configuration)
    {
        _auditingPersistence = auditingPersistence;
        _auditLoggingPolicyProvider = auditLoggingPolicyProvider;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _hardeningSecret = ResolveHardeningSecret(configuration);
        _hardeningKeyId = ResolveHardeningKeyId(configuration);
        _isHardeningEnabled = _hardeningSecret.Length >= MinimumHardeningSecretLength;

        if (!_isHardeningEnabled)
        {
            _logger.LogWarning(
                "Audit hardening is disabled because Auditing:Hardening:HmacSecret/JwtSettings:Secret is missing or shorter than {MinLength} characters.",
                MinimumHardeningSecretLength);
        }
    }

    public async Task SaveAsync(AuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        // Auditing should be best-effort and not depend on client disconnect timing.
        var cancellationToken = CancellationToken.None;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            auditLog.UserId ??= ResolveUserId(httpContext);
            auditLog.ClientIpAddress ??= ResolveClientIpAddress(httpContext);
            auditLog.BrowserInfo ??= httpContext.Request?.Headers["User-Agent"].ToString();
        }

        Normalize(auditLog);

        if (!await ShouldPersistAsync(auditLog, cancellationToken))
        {
            return;
        }

        try
        {
            var auditLogId = await _auditingPersistence.InsertAppAuditLogAsync(auditLog, cancellationToken);
            if (_isHardeningEnabled)
            {
                await TryAppendIntegrityAsync(
                    AppAuditLogsTableName,
                    auditLogId,
                    auditLog,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Auditing should not break the primary business workflow.
            _logger.LogError(
                ex,
                "Failed to persist audit log for {ServiceName}.{MethodName}",
                auditLog.ServiceName,
                auditLog.MethodName);
        }
    }

    private async Task<bool> ShouldPersistAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        var policy = await _auditLoggingPolicyProvider.GetCurrentPolicyAsync(cancellationToken);
        var classification = AuditLogClassifier.Classify(auditLog);

        if (classification.IsError && policy.LogErrorEvents)
        {
            return true;
        }

        return classification.OperationKind switch
        {
            AuditOperationKind.Read => policy.LogReadOperations,
            AuditOperationKind.Create => policy.LogCreateOperations,
            AuditOperationKind.Update => policy.LogUpdateOperations,
            AuditOperationKind.Delete => policy.LogDeleteOperations,
            _ => policy.LogOtherOperations,
        };
    }


    private async Task TryAppendIntegrityAsync(
        string auditTable,
        long auditLogId,
        AuditLog auditLog,
        CancellationToken cancellationToken)
    {
        if (auditLogId <= 0)
        {
            return;
        }

        await AuditIntegrityWriteLock.WaitAsync(cancellationToken);
        try
        {
            var previousHash = await _auditingPersistence.GetPreviousIntegrityHashAsync(auditTable, cancellationToken)
                ?? string.Empty;

            var currentHash = AuditIntegrityHasher.ComputeHash(
                _hardeningSecret,
                auditTable,
                previousHash,
                auditLogId,
                auditLog.UserId,
                auditLog.ServiceName,
                auditLog.MethodName,
                auditLog.Parameters,
                auditLog.ExecutionTime,
                auditLog.ExecutionDuration,
                auditLog.ClientIpAddress,
                auditLog.BrowserInfo,
                auditLog.Exception);

            await _auditingPersistence.AppendIntegrityAsync(new AuditLogIntegrity
            {
                AuditTable = auditTable,
                AuditLogId = auditLogId,
                PreviousHash = previousHash,
                CurrentHash = currentHash,
                Algorithm = IntegrityAlgorithm,
                KeyId = _hardeningKeyId,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist audit integrity chain record for {AuditTable}:{AuditLogId}",
                auditTable,
                auditLogId);
        }
        finally
        {
            AuditIntegrityWriteLock.Release();
        }
    }

    private static string? ResolveUserId(HttpContext httpContext)
    {
        return httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? httpContext.User?.FindFirst("id")?.Value
               ?? httpContext.User?.FindFirst("sub")?.Value
               ?? httpContext.User?.FindFirst("userId")?.Value;
    }

    private static string? ResolveClientIpAddress(HttpContext httpContext)
    {
        var remoteAddress = httpContext.Connection?.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(remoteAddress))
        {
            return remoteAddress;
        }

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].ToString();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp;
        }

        return null;
    }

    private static void Normalize(AuditLog auditLog)
    {
        if (auditLog.ExecutionTime == default)
        {
            auditLog.ExecutionTime = DateTime.UtcNow;
        }

        if (auditLog.ExecutionDuration < 0)
        {
            auditLog.ExecutionDuration = 0;
        }

        auditLog.ServiceName = Truncate(
            string.IsNullOrWhiteSpace(auditLog.ServiceName) ? "UnknownService" : auditLog.ServiceName.Trim(),
            ServiceNameMaxLength) ?? "UnknownService";

        auditLog.MethodName = Truncate(
            string.IsNullOrWhiteSpace(auditLog.MethodName) ? "UnknownMethod" : auditLog.MethodName.Trim(),
            MethodNameMaxLength) ?? "UnknownMethod";

        auditLog.Parameters = Truncate(auditLog.Parameters, ParametersMaxLength) ?? string.Empty;
        auditLog.ClientIpAddress = Truncate(auditLog.ClientIpAddress, ClientIpAddressMaxLength);
        auditLog.BrowserInfo = Truncate(auditLog.BrowserInfo, BrowserInfoMaxLength);
    }

    private static string ResolveHardeningSecret(IConfiguration configuration)
    {
        var secret = configuration["Auditing:Hardening:HmacSecret"];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            return secret.Trim();
        }

        var jwtSecret = configuration["JwtSettings:Secret"];
        if (!string.IsNullOrWhiteSpace(jwtSecret))
        {
            return jwtSecret.Trim();
        }

        return string.Empty;
    }

    private static string ResolveHardeningKeyId(IConfiguration configuration)
    {
        var keyId = configuration["Auditing:Hardening:KeyId"];
        return string.IsNullOrWhiteSpace(keyId) ? "v1" : keyId.Trim();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
