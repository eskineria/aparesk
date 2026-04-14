using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Auditing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Eskineria.Core.Auditing.Filters;

public sealed class AuditActionFilter : IAsyncActionFilter
{
    private const int StringPreviewLength = 120;
    private const string RedactedValue = "***REDACTED***";
    private static readonly string[] SensitiveArgumentNameParts =
    [
        "password",
        "secret",
        "token",
        "refresh",
        "access",
        "apikey",
        "api_key",
        "authorization",
        "cookie"
    ];

    private readonly IAuditingStore _auditingStore;
    private readonly ILogger<AuditActionFilter> _logger;

    public AuditActionFilter(
        IAuditingStore auditingStore,
        ILogger<AuditActionFilter> logger)
    {
        _auditingStore = auditingStore;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executionTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        ActionExecutedContext? executedContext = null;

        try
        {
            executedContext = await next();
        }
        finally
        {
            stopwatch.Stop();

            try
            {
                var statusCode = ResolveStatusCode(context.HttpContext, executedContext);
                var exceptionText = executedContext?.Exception is { } exception && !executedContext.ExceptionHandled
                    ? exception.ToString()
                    : null;
                var parameters = BuildParameters(context, statusCode);

                await _auditingStore.SaveAsync(new AuditLog
                {
                    ServiceName = ResolveServiceName(context),
                    MethodName = ResolveMethodName(context),
                    Parameters = parameters,
                    ExecutionTime = executionTime,
                    ExecutionDuration = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue),
                    Exception = exceptionText
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to write action audit log for {ActionDisplayName}",
                    context.ActionDescriptor.DisplayName);
            }
        }
    }

    private static int ResolveStatusCode(HttpContext httpContext, ActionExecutedContext? executedContext)
    {
        if (executedContext?.Result is IStatusCodeActionResult statusCodeActionResult &&
            statusCodeActionResult.StatusCode.HasValue)
        {
            return statusCodeActionResult.StatusCode.Value;
        }

        if (executedContext?.Exception is not null && !executedContext.ExceptionHandled)
        {
            return StatusCodes.Status500InternalServerError;
        }

        return httpContext.Response.StatusCode == 0
            ? StatusCodes.Status200OK
            : httpContext.Response.StatusCode;
    }

    private static string ResolveServiceName(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            return descriptor.ControllerTypeInfo.Name;
        }

        if (context.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller) &&
            !string.IsNullOrWhiteSpace(controller))
        {
            return controller.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
                ? controller
                : $"{controller}Controller";
        }

        return "UnknownController";
    }

    private static string ResolveMethodName(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            return descriptor.ActionName;
        }

        if (context.ActionDescriptor.RouteValues.TryGetValue("action", out var action) &&
            !string.IsNullOrWhiteSpace(action))
        {
            return action;
        }

        return context.ActionDescriptor.DisplayName ?? "UnknownAction";
    }

    private static string BuildParameters(ActionExecutingContext context, int statusCode)
    {
        var request = context.HttpContext.Request;
        var path = request.Path.HasValue ? request.Path.Value : "/";

        var args = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in context.ActionArguments)
        {
            args[name] = IsSensitiveName(name)
                ? RedactedValue
                : FormatArgumentValue(value);
        }

        var queryKeys = request.Query.Count == 0
            ? []
            : request.Query.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();

        return JsonSerializer.Serialize(new
        {
            Before = (object?)null,
            After = new Dictionary<string, object?>
            {
                ["HttpMethod"] = request.Method,
                ["Path"] = path,
                ["QueryKeys"] = queryKeys,
                ["StatusCode"] = statusCode,
                ["Args"] = args
            }
        });
    }

    private static bool IsSensitiveName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var part in SensitiveArgumentNameParts)
        {
            if (value.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatArgumentValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        return value switch
        {
            string text => $"\"{Truncate(text, StringPreviewLength)}\"",
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            bool boolValue => boolValue ? "true" : "false",
            Enum enumValue => enumValue.ToString(),
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? value.GetType().Name,
            IFormFile formFile => $"IFormFile(Name={formFile.Name}, FileName={formFile.FileName}, Length={formFile.Length})",
            IFormFileCollection formFileCollection => $"IFormFileCollection(Count={formFileCollection.Count})",
            IEnumerable enumerable when value is not string => SummarizeEnumerable(value, enumerable),
            _ => value.GetType().Name
        };
    }

    private static string SummarizeEnumerable(object value, IEnumerable enumerable)
    {
        if (enumerable is ICollection collection)
        {
            return $"{value.GetType().Name}(Count={collection.Count})";
        }

        return value.GetType().Name;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..maxLength]}...";
    }
}
