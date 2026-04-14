using System.Text.Json;
using Eskineria.Core.Auditing.Models;

namespace Eskineria.Core.Auditing.Utilities;

public static class AuditLogClassifier
{
    private static readonly string[] DeleteKeywords = ["delete", "remove", "purge", "revoke"];
    private static readonly string[] UpdateKeywords = ["update", "edit", "modify", "change", "reset", "activate", "deactivate", "confirm", "verify"];
    private static readonly string[] CreateKeywords = ["create", "add", "insert", "register", "accept", "upload", "generate"];
    private static readonly string[] ReadKeywords = ["get", "list", "find", "search", "query", "load", "check", "has", "preview", "evaluate"];

    public static AuditLogClassification Classify(AuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        var compositeName = $"{auditLog.ServiceName}.{auditLog.MethodName}".ToLowerInvariant();
        var httpMethod = TryResolveHttpMethod(auditLog.Parameters);

        var operationKind = ResolveOperationKind(compositeName, httpMethod);
        var isError = !string.IsNullOrWhiteSpace(auditLog.Exception);

        return new AuditLogClassification(operationKind, isError);
    }

    private static AuditOperationKind ResolveOperationKind(string compositeName, string? httpMethod)
    {
        if (ContainsAny(compositeName, DeleteKeywords))
        {
            return AuditOperationKind.Delete;
        }

        if (ContainsAny(compositeName, UpdateKeywords))
        {
            return AuditOperationKind.Update;
        }

        if (ContainsAny(compositeName, CreateKeywords))
        {
            return AuditOperationKind.Create;
        }

        if (ContainsAny(compositeName, ReadKeywords))
        {
            return AuditOperationKind.Read;
        }

        return httpMethod?.ToUpperInvariant() switch
        {
            "GET" => AuditOperationKind.Read,
            "PUT" => AuditOperationKind.Update,
            "PATCH" => AuditOperationKind.Update,
            "DELETE" => AuditOperationKind.Delete,
            _ => AuditOperationKind.Other,
        };
    }

    private static bool ContainsAny(string value, string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (value.Contains(keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? TryResolveHttpMethod(string? parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(parameters);
            if (!document.RootElement.TryGetProperty("After", out var afterElement))
            {
                return null;
            }

            if (!afterElement.TryGetProperty("HttpMethod", out var httpMethodElement))
            {
                return null;
            }

            return httpMethodElement.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
