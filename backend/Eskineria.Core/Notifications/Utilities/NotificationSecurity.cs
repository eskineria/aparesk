using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Eskineria.Core.Notifications.Utilities;

public static class NotificationSecurity
{
    private const int MaxErrorMessageLength = 1024;
    private static readonly Regex ControlCharsRegex = new(@"[\r\n\t]+", RegexOptions.Compiled);

    public static string NormalizeAndValidateEmail(string email, string paramName = "email")
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email address is required.", paramName);
        }

        var normalized = email.Trim();
        if (normalized.Length > 320)
        {
            throw new ArgumentException("Email address exceeds maximum allowed length.", paramName);
        }

        try
        {
            var parsed = new MailAddress(normalized);
            if (!string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Email address format is invalid.", paramName);
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException("Email address format is invalid.", paramName);
        }

        return normalized;
    }

    public static string NormalizeSubject(string subject, string paramName = "subject")
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject cannot be empty.", paramName);
        }

        var normalized = SanitizeSingleLine(subject.Trim());
        if (normalized.Length > 500)
        {
            normalized = normalized[..500];
        }

        return normalized;
    }

    public static string NormalizeBody(string body, string paramName = "body")
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Body cannot be empty.", paramName);
        }

        return body;
    }

    public static string SanitizeErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return "Notification dispatch failed.";
        }

        var normalized = ControlCharsRegex.Replace(errorMessage.Trim(), " ");
        if (normalized.Length > MaxErrorMessageLength)
        {
            normalized = normalized[..MaxErrorMessageLength];
        }

        return normalized;
    }

    public static string MaskEmailForLog(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "***";
        }

        var normalized = email.Trim();
        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 1 || atIndex == normalized.Length - 1)
        {
            return "***";
        }

        var userPart = normalized[..atIndex];
        var domainPart = normalized[(atIndex + 1)..];
        var maskedUser = $"{userPart[0]}***{userPart[^1]}";

        return $"{maskedUser}@{domainPart}";
    }

    private static string SanitizeSingleLine(string value)
        => ControlCharsRegex.Replace(value, " ");
}
