using System.ComponentModel.DataAnnotations;

namespace Eskineria.Core.Auth.Models;

public class SocialLoginRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty; // e.g., "Google", "Facebook"

    [Required]
    public string IdToken { get; set; } = string.Empty;

    public string? MfaCode { get; set; }
}
