using Eskineria.Core.Shared.Exceptions;

namespace Eskineria.Core.Compliance.Entities;

/// <summary>
/// Represents a version of Terms and Conditions or Privacy Policy
/// </summary>
public class TermsAndConditions
{
    private const int MaxTypeLength = 50;
    private const int MaxVersionLength = 20;
    private const int MaxSummaryLength = 500;

    public Guid Id { get; set; }
    
    /// <summary>
    /// Type of terms (e.g., "TermsOfService", "PrivacyPolicy", "CookiePolicy")
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Version number (e.g., "1.0", "2.0", "2.1")
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Full content of the terms (can be HTML or Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Short summary or title
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// When this version becomes effective
    /// </summary>
    public DateTime EffectiveDate { get; set; }
    
    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Is this the currently active version?
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// User acceptances for this version
    /// </summary>
    public ICollection<UserTermsAcceptance> UserAcceptances { get; set; } = new List<UserTermsAcceptance>();

    public static TermsAndConditions Create(
        string type,
        string version,
        string content,
        string? summary,
        DateTime effectiveDate,
        DateTime? createdAtUtc = null)
    {
        EnsureType(type);
        EnsureVersion(version);
        EnsureContent(content);

        return new TermsAndConditions
        {
            Id = Guid.NewGuid(),
            Type = type.Trim(),
            Version = version.Trim(),
            Content = content.Trim(),
            Summary = NormalizeSummary(summary),
            EffectiveDate = effectiveDate,
            CreatedAt = createdAtUtc ?? DateTime.UtcNow,
            IsActive = false
        };
    }

    public void UpdateContent(string content, string? summary, bool isActive)
    {
        EnsureContent(content);
        Content = content.Trim();
        Summary = NormalizeSummary(summary);
        IsActive = isActive;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string? NormalizeSummary(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        var normalized = summary.Trim();
        if (normalized.Length > MaxSummaryLength)
        {
            throw new DomainException($"Terms summary cannot exceed {MaxSummaryLength} characters.");
        }

        return normalized;
    }

    private static void EnsureType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new DomainException("Terms type is required.");
        }

        if (type.Trim().Length > MaxTypeLength)
        {
            throw new DomainException($"Terms type cannot exceed {MaxTypeLength} characters.");
        }
    }

    private static void EnsureVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new DomainException("Terms version is required.");
        }

        if (version.Trim().Length > MaxVersionLength)
        {
            throw new DomainException($"Terms version cannot exceed {MaxVersionLength} characters.");
        }
    }

    private static void EnsureContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Terms content is required.");
        }
    }
}
