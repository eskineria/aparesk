using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Eskineria.Core.Logging.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Eskineria.Core.Logging.Middlewares;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly EskineriaLoggingOptions _options;
    private readonly HashSet<string> _exactMaskedFields;
    private readonly string[] _containsMaskedFields;
    private readonly HashSet<string> _sensitiveHeaders;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IOptions<EskineriaLoggingOptions>? options = null)
    {
        _next = next;
        _logger = logger;
        _options = options?.Value ?? new EskineriaLoggingOptions();
        _sensitiveHeaders = new HashSet<string>(_options.SensitiveHeaders, StringComparer.OrdinalIgnoreCase);
        (_exactMaskedFields, _containsMaskedFields) = BuildMaskedFieldMatchers(_options.MaskedFields);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (ShouldExclude(path))
        {
            await _next(context);
            return;
        }

        var requestContentType = context.Request.ContentType ?? string.Empty;
        var requestIsBinary = IsBinaryContent(requestContentType);
        var maskedHeaders = MaskSensitiveHeaders(context.Request.Headers);
        var stopwatch = Stopwatch.StartNew();

        if (CanLogRequestBody(context.Request, requestIsBinary))
        {
            context.Request.EnableBuffering();
            var requestBody = await ReadRequestBodyAsync(context.Request);
            _logger.LogInformation(
                "Incoming Request: {Method} {Path} {Query} Headers: {Headers} Body: {Body}",
                context.Request.Method,
                context.Request.Path,
                MaskSensitiveQueryString(context.Request.Query),
                maskedHeaders,
                MaskSensitiveData(requestBody));
            context.Request.Body.Position = 0;
        }
        else
        {
            _logger.LogInformation(
                "Incoming Request: {Method} {Path} {Query} Headers: {Headers} [BODY_SKIPPED]",
                context.Request.Method,
                context.Request.Path,
                MaskSensitiveQueryString(context.Request.Query),
                maskedHeaders);
        }

        if (!_options.EnableRequestResponseBodyLogging || context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            stopwatch.Stop();
            _logger.LogInformation(
                "Outgoing Response: {StatusCode} ({ElapsedMs} ms)",
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
            return;
        }

        var originalBodyStream = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var responseCopied = false;

        try
        {
            await _next(context);

            var responseContentType = context.Response.ContentType ?? string.Empty;
            var responseIsBinary = IsBinaryContent(responseContentType);

            if (!responseIsBinary &&
                responseBody.Length <= _options.MaxBodyLogSizeBytes &&
                IsLoggableTextContent(responseContentType) &&
                !ShouldSkipBodyLoggingForPath(context.Request.Path))
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                _logger.LogInformation(
                    "Outgoing Response: {StatusCode} {Body}",
                    context.Response.StatusCode,
                    MaskSensitiveData(responseText));
            }
            else
            {
                _logger.LogInformation("Outgoing Response: {StatusCode} [BODY_SKIPPED]", context.Response.StatusCode);
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            responseCopied = true;
        }
        finally
        {
            if (!responseCopied)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }

            context.Response.Body = originalBodyStream;
            stopwatch.Stop();
            _logger.LogDebug("Request completed in {ElapsedMs} ms for {Path}", stopwatch.ElapsedMilliseconds, context.Request.Path);
        }
    }

    private bool CanLogRequestBody(HttpRequest request, bool isBinary)
    {
        if (isBinary)
        {
            return false;
        }

        if (ShouldSkipBodyLoggingForPath(request.Path))
        {
            return false;
        }

        if (!IsLoggableTextContent(request.ContentType ?? string.Empty))
        {
            return false;
        }

        if (request.HasFormContentType ||
            (request.ContentType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (request.ContentType?.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return false;
        }

        if (request.ContentLength.HasValue && request.ContentLength.Value > _options.MaxBodyLogSizeBytes)
        {
            return false;
        }

        return request.ContentLength.HasValue;
    }

    private bool ShouldExclude(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var prefix in _options.ExcludedPathPrefixes)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                continue;
            }

            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBinaryContent(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.Contains("image", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("audio", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("video", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("octet-stream", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLoggableTextContent(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("application/xml", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("text/", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private string MaskSensitiveData(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return json;
            }

            MaskNode(node);
            return node.ToString();
        }
        catch
        {
            return "[UNPARSABLE_BODY]";
        }
    }

    private void MaskNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (IsSensitiveFieldName(property.Key))
                {
                    obj[property.Key] = "***MASKED***";
                }
                else if (property.Value is JsonObject || property.Value is JsonArray)
                {
                    MaskNode(property.Value);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is not null)
                {
                    MaskNode(item);
                }
            }
        }
    }

    private bool IsSensitiveFieldName(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        var normalizedFieldName = NormalizeFieldName(fieldName);
        if (string.IsNullOrWhiteSpace(normalizedFieldName))
        {
            return false;
        }

        if (_exactMaskedFields.Contains(normalizedFieldName))
        {
            return true;
        }

        foreach (var maskedField in _containsMaskedFields)
        {
            if (normalizedFieldName.Contains(maskedField, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private string MaskSensitiveQueryString(IQueryCollection query)
    {
        if (query.Count == 0)
        {
            return string.Empty;
        }

        var sanitizedPairs = query.SelectMany(entry =>
        {
            var value = IsSensitiveFieldName(entry.Key) ? "***MASKED***" : TruncateForLog(SanitizeForLog(entry.Value), _options.MaxQueryValueLogLength);
            return new[] { $"{Uri.EscapeDataString(entry.Key)}={Uri.EscapeDataString(value)}" };
        });

        var rendered = $"?{string.Join("&", sanitizedPairs)}";
        return TruncateForLog(rendered, _options.MaxQueryStringLogLength);
    }

    private static bool ShouldSkipBodyLoggingForPath(PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        var segments = path.Value!
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Any(segment => string.Equals(segment, "auth", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeFieldName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.ToString();
    }

    private string MaskSensitiveHeaders(IHeaderDictionary headers)
    {
        var maskedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            if (_sensitiveHeaders.Contains(header.Key))
            {
                maskedHeaders[header.Key] = "***MASKED***";
            }
            else
            {
                var value = SanitizeForLog(header.Value);
                maskedHeaders[header.Key] = TruncateForLog(value, _options.MaxHeaderValueLogLength);
            }
        }

        return JsonSerializer.Serialize(maskedHeaders);
    }

    private static (HashSet<string> Exact, string[] Contains) BuildMaskedFieldMatchers(IEnumerable<string> maskedFields)
    {
        var exact = new HashSet<string>(StringComparer.Ordinal);
        var contains = new List<string>();

        foreach (var field in maskedFields)
        {
            var normalized = NormalizeFieldName(field);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            exact.Add(normalized);
            if (normalized.Length >= 5)
            {
                contains.Add(normalized);
            }
        }

        return (exact, contains.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static string TruncateForLog(string value, int maxLength)
    {
        if (maxLength <= 0 || value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..maxLength]}...";
    }

    private static string SanitizeForLog(StringValues values)
    {
        return SanitizeForLog(values.ToString());
    }

    private static string SanitizeForLog(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }
}
