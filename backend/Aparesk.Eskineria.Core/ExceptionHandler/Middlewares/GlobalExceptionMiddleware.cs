using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Aparesk.Eskineria.Core.ExceptionHandler.Configuration;
using Aparesk.Eskineria.Core.ExceptionHandler.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Aparesk.Eskineria.Core.ExceptionHandler.Middlewares;

public class GlobalExceptionMiddleware
{
    private const int ClientClosedRequestStatusCode = 499;

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;
    private readonly ExceptionOptions _options;
    private readonly IStringLocalizer<GlobalExceptionMiddleware>? _localizer;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger, 
        IHostEnvironment env,
        ExceptionOptions? configuredOptions = null,
        IOptions<ExceptionOptions>? options = null,
        IStringLocalizer<GlobalExceptionMiddleware>? localizer = null)
    {
        _next = next;
        _logger = logger;
        _env = env;
        _options = configuredOptions ?? options?.Value ?? new ExceptionOptions();
        _localizer = localizer;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            if (httpContext.Response.HasStarted)
            {
                _logger.LogWarning(ex, "The response has already started, exception handler middleware will rethrow.");
                throw;
            }

            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.Clear();
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.CacheControl = "no-store";

        var exceptionDetails = GetExceptionDetails(context, exception);
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        LogException(exception, exceptionDetails.StatusCode, traceId);
        
        context.Response.StatusCode = exceptionDetails.StatusCode;

        ProblemDetails problemDetails = exceptionDetails.Errors != null
            ? new ValidationProblemDetails(exceptionDetails.Errors)
            {
                Status = exceptionDetails.StatusCode, Title = exceptionDetails.Title, Detail = exceptionDetails.Detail, Instance = context.Request.Path
            }
            : new ProblemDetails
            {
                Status = exceptionDetails.StatusCode, Title = exceptionDetails.Title, Detail = exceptionDetails.Detail, Instance = context.Request.Path
            };

        problemDetails.Type = $"https://httpstatuses.com/{exceptionDetails.StatusCode}";

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestampUtc"] = DateTime.UtcNow;

        if (exception is BaseCustomException baseEx && !string.IsNullOrEmpty(baseEx.ErrorCode))
        {
            problemDetails.Extensions["errorCode"] = baseEx.ErrorCode;
        }
        else if (TryGetMapping(exception, out var mappingConfig) &&
                 mappingConfig is not null &&
                 !string.IsNullOrEmpty(mappingConfig.ErrorCode))
        {
            problemDetails.Extensions["errorCode"] = mappingConfig.ErrorCode;
        }

        if (ShouldIncludeExceptionDetails())
        {
            problemDetails.Extensions["exception"] = exception.ToString();
        }
        
        if (_options.OnBeforeWriteResponse != null)
        {
            try
            {
                await _options.OnBeforeWriteResponse(context, exception, problemDetails);
            }
            catch (Exception callbackException)
            {
                _logger.LogError(
                    callbackException,
                    "Exception callback failed. Original exception traceId={TraceId}, statusCode={StatusCode}",
                    traceId,
                    exceptionDetails.StatusCode);
            }
        }

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problemDetails,
            problemDetails.GetType(),
            DefaultJsonOptions,
            context.RequestAborted);
    }
    
    private ExceptionDetails GetExceptionDetails(HttpContext context, Exception exception)
    {
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var title = GetLocalizedValue("UnhandledExceptionTitle", "An error occurred while processing your request.");
        var detail = GetLocalizedValue("InternalServerErrorDetail", "An unexpected error occurred.");

        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            return new ExceptionDetails(
                ClientClosedRequestStatusCode,
                "Client Closed Request",
                "The request was canceled by the client.",
                null);
        }

        if (exception is BadHttpRequestException badRequestException)
        {
            return new ExceptionDetails(
                badRequestException.StatusCode,
                ReasonPhrases.GetReasonPhrase(badRequestException.StatusCode),
                badRequestException.Message,
                null);
        }

        if (exception is IValidationException validationEx)
        {
            return new ExceptionDetails(
                (int)HttpStatusCode.BadRequest,
                GetLocalizedValue("ValidationErrorTitle", "Validation Error"),
                exception.Message,
                validationEx.Errors);
        }
        
        if (TryGetMapping(exception, out var mappingConfig) && mappingConfig is not null)
        {
            statusCode = mappingConfig.StatusCode;
            title = !string.IsNullOrEmpty(mappingConfig.Title) 
                ? mappingConfig.Title 
                : ReasonPhrases.GetReasonPhrase(statusCode);

            detail = (_env.IsDevelopment() || statusCode < 500)
                ? exception.Message 
                : GetLocalizedValue("InternalServerErrorDetail", "An unexpected error occurred.");
                
            return new ExceptionDetails(statusCode, title, detail, null);
        }
        
        if (exception is BaseCustomException customEx)
        {
            var exposeMessage = _env.IsDevelopment() || customEx.StatusCode < 500;
            var safeTitle = exposeMessage
                ? ReasonPhrases.GetReasonPhrase(customEx.StatusCode)
                : GetLocalizedValue("InternalServerErrorTitle", "Internal Server Error");
            var safeDetail = exposeMessage
                ? customEx.Message
                : GetLocalizedValue("InternalServerErrorDetail", "An unexpected error occurred.");
            return new ExceptionDetails(customEx.StatusCode, safeTitle, safeDetail, null);
        }
        
        if (!_env.IsDevelopment())
        {
            title = GetLocalizedValue("InternalServerErrorTitle", "Internal Server Error");
        }
        else
        {
            detail = exception.Message;
        }
        
        return new ExceptionDetails(statusCode, title, detail, null);
    }

    private bool TryGetMapping(Exception exception, out ExceptionMappingConfig? mappingConfig)
    {
        Type? currentType = exception.GetType();

        while (currentType is not null && currentType != typeof(object))
        {
            var lookupType = currentType;
            if (_options.ExceptionMappings.TryGetValue(lookupType, out mappingConfig))
            {
                return true;
            }

            currentType = lookupType.BaseType;
        }

        mappingConfig = null;
        return false;
    }

    private string GetLocalizedValue(string key, string fallback)
    {
        var value = _localizer?[key].Value;
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal))
        {
            return fallback;
        }

        return value;
    }

    private bool ShouldIncludeExceptionDetails()
    {
        return _options.IncludeExceptionDetailsInResponse || _env.IsDevelopment();
    }

    private void LogException(Exception exception, int statusCode, string traceId)
    {
        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception. traceId={TraceId}, statusCode={StatusCode}", traceId, statusCode);
            return;
        }

        _logger.LogWarning(exception, "Request failed. traceId={TraceId}, statusCode={StatusCode}", traceId, statusCode);
    }

    private sealed record ExceptionDetails(
        int StatusCode,
        string Title,
        string Detail,
        IDictionary<string, string[]>? Errors);
}
