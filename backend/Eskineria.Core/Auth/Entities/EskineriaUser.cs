using Microsoft.AspNetCore.Identity;

namespace Eskineria.Core.Auth.Entities;

public class EskineriaUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? ProfilePicture { get; set; }
    public string? ActiveRole { get; set; }
    public string? EmailVerificationCodeHash { get; set; }
    public DateTime? EmailVerificationCodeExpiresAtUtc { get; set; }
    public DateTime? EmailVerificationCodeSentAtUtc { get; set; }
    public int EmailVerificationFailedAttempts { get; set; }
    public string? PasswordResetCodeHash { get; set; }
    public DateTime? PasswordResetCodeExpiresAtUtc { get; set; }
    public DateTime? PasswordResetCodeSentAtUtc { get; set; }
    public int PasswordResetFailedAttempts { get; set; }
    
    /// <summary>
    /// User's acceptance history for different terms versions
    /// Note: This creates a circular dependency with Domain layer
    /// Consider using ICollection<object> or removing if not needed in Auth context
    /// </summary>
    // public ICollection<UserTermsAcceptance> TermsAcceptances { get; set; } = new List<UserTermsAcceptance>();
}
