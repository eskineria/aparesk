using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Eskineria.Core.Logging.Configuration;
using Serilog.Context;

namespace Eskineria.Core.Logging.Middlewares;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly EskineriaLoggingOptions _options;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        IOptions<EskineriaLoggingOptions>? options = null)
    {
        _next = next;
        _options = options?.Value ?? new EskineriaLoggingOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headerName = _options.CorrelationIdHeaderName;
        var correlationId = context.Request.Headers[headerName].FirstOrDefault();

        if (!IsValidCorrelationId(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        var safeCorrelationId = correlationId!;
        context.Response.Headers[headerName] = safeCorrelationId;
        context.TraceIdentifier = safeCorrelationId;

        using (LogContext.PushProperty("CorrelationId", safeCorrelationId))
        {
            await _next(context);
        }
    }

    private static bool IsValidCorrelationId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Length > 128)
        {
            return false;
        }

        return value.All(ch =>
            char.IsLetterOrDigit(ch) ||
            ch == '-' ||
            ch == '_' ||
            ch == '.' ||
            ch == ':');
    }
}
