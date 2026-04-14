namespace Eskineria.Core.Auth.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool Used { get; set; }
    public bool Invalidated { get; set; }
    public DateTime? InvalidatedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid UserId { get; set; }
    
    // Navigation property if you want to link strictly, 
    // but typically managed via IdentityUser extension or separate table
    // For this implementation, we can link it to EskineriaUser
    public EskineriaUser? User { get; set; }
}
