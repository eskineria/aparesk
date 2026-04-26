using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Aparesk.Eskineria.Core.Auditing.Utilities;

public static class AuditIntegrityHasher
{
    public static string ComputeHash(
        string secret,
        string auditTable,
        string previousHash,
        long auditLogId,
        string? userId,
        string serviceName,
        string methodName,
        string parameters,
        DateTime executionTimeUtc,
        int executionDuration,
        string? clientIpAddress,
        string? browserInfo,
        string? exception)
    {
        var canonical = string.Join("|",
            Escape(auditTable),
            Escape(previousHash),
            auditLogId.ToString(CultureInfo.InvariantCulture),
            Escape(userId),
            Escape(serviceName),
            Escape(methodName),
            Escape(parameters),
            executionTimeUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            executionDuration.ToString(CultureInfo.InvariantCulture),
            Escape(clientIpAddress),
            Escape(browserInfo),
            Escape(exception));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Escape(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
