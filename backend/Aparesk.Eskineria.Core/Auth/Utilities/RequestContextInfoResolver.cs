using Microsoft.AspNetCore.Http;

namespace Aparesk.Eskineria.Core.Auth.Utilities;

public static class RequestContextInfoResolver
{
    public static string? ResolveClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return null;
        }

        var remoteAddress = httpContext.Connection.RemoteIpAddress?.ToString();
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
        return string.IsNullOrWhiteSpace(realIp) ? null : realIp;
    }

    public static string? ResolveUserAgent(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return null;
        }

        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
    }
}
