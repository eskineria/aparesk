using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Aparesk.Eskineria.Core.RateLimit.Models;
using Aparesk.Eskineria.Core.Shared.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Aparesk.Eskineria.Core.RateLimit.Handlers;

public static class RateLimitResponseHandler
{
    public static async ValueTask OnRejected(OnRejectedContext context, CancellationToken token)
    {
        if (context.HttpContext.Response.HasStarted)
        {
            return;
        }

        var retryAfterSeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        context.HttpContext.Response.Headers.CacheControl = "no-store, no-cache";
        context.HttpContext.Response.Headers.Pragma = "no-cache";
        context.HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";

        var responseData = new RateLimitRejectionData
        {
            RetryAfterSeconds = retryAfterSeconds,
            TraceId = context.HttpContext.TraceIdentifier
        };

        var localizedKey = "RateLimitExceeded";
        var fallbackMessage = "Rate limit exceeded. Please try again later.";

        var response = new DataResponse<RateLimitRejectionData>(
            responseData,
            success: false,
            message: GetLocalizedMessage(context.HttpContext, localizedKey, fallbackMessage),
            statusCode: (int)HttpStatusCode.TooManyRequests);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.HttpContext.Response.WriteAsync(json, token);
    }

    private static string GetLocalizedMessage(HttpContext httpContext, string key, string fallback)
    {
        var localizerFactory = httpContext.RequestServices.GetService<IStringLocalizerFactory>();
        var localizer = localizerFactory?.Create(typeof(RateLimitResponseHandler));
        if (localizer == null)
        {
            return fallback;
        }

        var localized = localizer[key].Value;
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
