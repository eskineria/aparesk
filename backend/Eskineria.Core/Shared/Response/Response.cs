using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Eskineria.Core.Shared.Response;

public class Response : IResponse
{
    private const int DefaultSuccessStatusCode = 200;
    private const int DefaultFailureStatusCode = 400;
    private static readonly Regex UnsafeControlCharsRegex = new("[\\u0000-\\u0008\\u000B\\u000C\\u000E-\\u001F\\u007F]", RegexOptions.Compiled);

    [JsonPropertyOrder(1)]
    public bool Success { get; set; }
    
    [JsonPropertyOrder(3)]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyOrder(2)]
    public int StatusCode { get; set; }

    public Response()
    {
    }

    public Response(bool success, string message, int statusCode)
    {
        Success = success;
        Message = NormalizeMessage(message, success);
        StatusCode = NormalizeStatusCode(statusCode, success);
    }

    public static Response Succeed(string message = "Success", int statusCode = DefaultSuccessStatusCode)
    {
        return new Response(true, message, statusCode);
    }

    public static Response Fail(string message, int statusCode = DefaultFailureStatusCode)
    {
        return new Response(false, message, statusCode);
    }

    private static string NormalizeMessage(string message, bool success)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            var sanitizedMessage = UnsafeControlCharsRegex.Replace(message.Trim(), string.Empty);
            if (!string.IsNullOrWhiteSpace(sanitizedMessage))
            {
                return sanitizedMessage;
            }
        }

        return success ? "Success" : "Operation failed.";
    }

    private static int NormalizeStatusCode(int statusCode, bool success)
    {
        // HTTP status range
        if (statusCode is < 100 or > 599)
        {
            return success ? DefaultSuccessStatusCode : DefaultFailureStatusCode;
        }

        // Keep semantic consistency between Success/Fail factories and status code families.
        if (success && (statusCode < 200 || statusCode >= 400))
        {
            return DefaultSuccessStatusCode;
        }

        if (!success && statusCode < 400)
        {
            return DefaultFailureStatusCode;
        }

        return statusCode;
    }
}
