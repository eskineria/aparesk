using System.ComponentModel.DataAnnotations;

namespace Eskineria.Core.Auth.Models;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? MfaCode { get; set; }
}

public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool TermsAccepted { get; set; }
    public bool PrivacyPolicyAccepted { get; set; }
}

public class AuthRuntimeSettings
{
    public bool EmailVerificationRequired { get; set; } = true;
    public int EmailVerificationCodeExpirySeconds { get; set; } = 180;
    public int EmailVerificationResendCooldownSeconds { get; set; } = 60;
    public int SessionAccessTokenLifetimeMinutes { get; set; } = 60;
    public int SessionRefreshTokenLifetimeDays { get; set; } = 7;
    public int SessionMaxActiveSessions { get; set; } = 5;
    public int SessionIdleTimeoutMinutes { get; set; } = 60;
    public int SessionWarningBeforeTimeoutMinutes { get; set; } = 2;
    public int SessionRememberMeDurationDays { get; set; } = 30;
    public bool SessionSingleDeviceModeEnabled { get; set; }
    public int PasswordMinLength { get; set; } = 8;
    public bool PasswordRequireUppercase { get; set; } = true;
    public bool PasswordRequireLowercase { get; set; } = true;
    public bool PasswordRequireDigit { get; set; } = true;
    public bool PasswordRequireNonAlphanumeric { get; set; }
    public bool MfaEnforcedForAll { get; set; }
    public int MfaTrustedDeviceDurationDays { get; set; } = 30;
    public string MfaBypassIpWhitelist { get; set; } = string.Empty;
    public bool RegistrationInvitationRequired { get; set; }
    public string RegistrationAllowedEmailDomains { get; set; } = string.Empty;
    public string RegistrationBlockedEmailDomains { get; set; } = string.Empty;
    public bool RegistrationAutoApproveEnabled { get; set; } = true;
    public bool LoginLockoutEnabled { get; set; } = true;
    public int LoginMaxFailedAttempts { get; set; } = 5;
    public int LoginLockoutDurationMinutes { get; set; } = 15;
    public bool MaintenanceModeEnabled { get; set; }
    public string MaintenanceIpWhitelist { get; set; } = string.Empty;
    public string MaintenanceRoleWhitelist { get; set; } = "Admin";
    public string RequestIpAddress { get; set; } = string.Empty;
    public bool NotificationLoginAlertEnabled { get; set; } = true;
    public bool MfaFeatureEnabled { get; set; }
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TokenResponse? Data { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

public class AuthResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}
