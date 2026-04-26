using System.Threading.RateLimiting;
using Aparesk.Eskineria.Core.RateLimit.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aparesk.Eskineria.Core.RateLimit.Middlewares;

/// <summary>
/// SECURITY: Rate limiting middleware to protect against DoS attacks
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly PartitionedRateLimiter<HttpContext> _rateLimiter;
    private readonly RateLimitOptions _options;

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger,
        RateLimitOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
        
        // Create partitioned rate limiter
        _rateLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var clientId = GetClientIdentifier(context);
            
            return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => 
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = _options.Global.PermitLimit,
                    Window = TimeSpan.FromSeconds(_options.Global.WindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = _options.Global.QueueLimit
                });
        });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var lease = await _rateLimiter.AcquireAsync(context, permitCount: 1);
        
        if (!lease.IsAcquired)
        {
            var clientId = GetClientIdentifier(context);
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            
            if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
            }
            else
            {
                context.Response.Headers.RetryAfter = "60";
            }
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too Many Requests",
                message = "Rate limit exceeded. Please try again later.",
                retryAfter = context.Response.Headers.RetryAfter.ToString()
            });
            
            return;
        }

        await _next(context);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // Try to get user ID from claims first
        var userId = context.User?.FindFirst("sub")?.Value 
                     ?? context.User?.FindFirst("userId")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";
        
        // Fallback to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Check for forwarded IP (behind proxy/load balancer)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            ipAddress = forwardedFor.ToString().Split(',')[0].Trim();
        }
        
        return $"ip:{ipAddress}";
    }
}
