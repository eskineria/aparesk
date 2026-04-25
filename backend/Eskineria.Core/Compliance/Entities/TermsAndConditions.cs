using Eskineria.Core.Shared.Exceptions;
using Eskineria.Core.Shared.Localization;

namespace Eskineria.Core.Compliance.Entities;

/// <summary>
/// Represents a version of Terms and Conditions or Privacy Policy with multi-language support
/// </summary>
public class TermsAndConditions
{
    private const int MaxTypeLength = 50;
    private const int MaxVersionLength = 20;

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
    /// Localized content of the terms (JSON mapping)
    /// </summary>
    public LocalizedContent Content { get; set; } = new();
    
    /// <summary>
    /// Localized summary or title (JSON mapping)
    /// </summary>
    public LocalizedContent Summary { get; set; } = new();
    
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
        LocalizedContent content,
        LocalizedContent? summary,
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
            Content = content,
            Summary = summary ?? new(),
            EffectiveDate = effectiveDate,
            CreatedAt = createdAtUtc ?? DateTime.UtcNow,
            IsActive = false
        };
    }

    public void UpdateContent(LocalizedContent content, LocalizedContent? summary, bool isActive)
    {
        EnsureContent(content);
        Content = content;
        Summary = summary ?? new();
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

    private static void EnsureContent(LocalizedContent content)
    {
        if (content == null || content.Count == 0 || content.All(x => string.IsNullOrWhiteSpace(x.Value)))
        {
            throw new DomainException("Terms content is required in at least one language.");
        }
    }
}
