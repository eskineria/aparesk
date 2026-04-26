using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Aparesk.Eskineria.Core.RateLimit.Utilities;

public static class RateLimitClientIdentifierResolver
{
    private const int MaxRawIdentifierLength = 512;

    public static string Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value
                         ?? context.User.FindFirst("userId")?.Value
                         ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{ToStableKey(userId)}";
            }
        }

        var ipAddress = GetIpAddress(context);
        return $"ip:{ToStableKey(ipAddress)}";
    }

    private static string GetIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string ToStableKey(string raw)
    {
        var normalized = string.IsNullOrWhiteSpace(raw) ? "unknown" : raw.Trim();
        if (normalized.Length > MaxRawIdentifierLength)
        {
            normalized = normalized[..MaxRawIdentifierLength];
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes[..16]).ToLowerInvariant();
    }
}
