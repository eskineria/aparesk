using Eskineria.Core.Shared.Exceptions;

namespace Eskineria.Core.Compliance.Entities;

/// <summary>
/// Represents a user's acceptance of a specific version of Terms and Conditions
/// </summary>
public class UserTermsAcceptance
{
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 500;

    public Guid Id { get; set; }
    
    /// <summary>
    /// User who accepted the terms (FK to AspNetUsers)
    /// </summary>
    public Guid UserId { get; set; }
    // Note: No navigation property to avoid circular dependency with Auth module
    
    /// <summary>
    /// The terms version that was accepted
    /// </summary>
    public Guid TermsAndConditionsId { get; set; }
    public TermsAndConditions TermsAndConditions { get; set; } = null!;
    
    /// <summary>
    /// When the user accepted these terms
    /// </summary>
    public DateTime AcceptedAt { get; set; }
    
    /// <summary>
    /// IP address from which the acceptance was made (for audit purposes)
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent string (browser/device info)
    /// </summary>
    public string? UserAgent { get; set; }

    public static UserTermsAcceptance Create(
        Guid userId,
        Guid termsAndConditionsId,
        string? ipAddress,
        string? userAgent,
        DateTime? acceptedAtUtc = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (termsAndConditionsId == Guid.Empty)
        {
            throw new DomainException("Terms and conditions id is required.");
        }

        return new UserTermsAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TermsAndConditionsId = termsAndConditionsId,
            AcceptedAt = acceptedAtUtc ?? DateTime.UtcNow,
            IpAddress = NormalizeIpAddress(ipAddress),
            UserAgent = NormalizeUserAgent(userAgent)
        };
    }

    private static string? NormalizeIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        var normalized = ipAddress.Trim();
        return normalized.Length <= MaxIpAddressLength
            ? normalized
            : normalized[..MaxIpAddressLength];
    }

    private static string? NormalizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        var normalized = userAgent.Trim();
        return normalized.Length <= MaxUserAgentLength
            ? normalized
            : normalized[..MaxUserAgentLength];
    }
}
