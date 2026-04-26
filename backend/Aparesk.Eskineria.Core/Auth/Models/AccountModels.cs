using System.ComponentModel.DataAnnotations;

namespace Aparesk.Eskineria.Core.Auth.Models;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class VerifyPasswordResetCodeRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ResendPasswordResetCodeRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ConfirmEmailRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class VerifyEmailCodeRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ResendEmailVerificationCodeRequest
{
    public string Email { get; set; } = string.Empty;
}

public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string? ProfilePicture { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? ActiveRole { get; set; }
    public bool HasPassword { get; set; }
}

public class MfaStatusDto
{
    public bool Enabled { get; set; }
    public bool FeatureEnabled { get; set; }
}

public class UpdateMfaRequest
{
    public bool Enabled { get; set; }
    public string? CurrentPassword { get; set; }
    public string? Code { get; set; }
}

public class SendMfaCodeRequest
{
    public bool TargetState { get; set; }
}

public class UpdateUserInfoRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
}

public class UserSessionDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsExpired { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
