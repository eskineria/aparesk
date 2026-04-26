namespace Aparesk.Eskineria.Core.Settings.Models;

public class AuthSystemSettingsDto
{
    public bool LoginEnabled { get; set; }
    public bool RegisterEnabled { get; set; }
    public bool GoogleLoginEnabled { get; set; }
    public bool ForgotPasswordEnabled { get; set; }
    public bool ChangePasswordEnabled { get; set; }
    public bool SessionManagementEnabled { get; set; }
    public bool EmailVerificationRequired { get; set; }
    public int EmailVerificationCodeExpirySeconds { get; set; }
    public int EmailVerificationResendCooldownSeconds { get; set; }
    public int SessionAccessTokenLifetimeMinutes { get; set; }
    public int SessionRefreshTokenLifetimeDays { get; set; }
    public int SessionMaxActiveSessions { get; set; }
    public int SessionIdleTimeoutMinutes { get; set; }
    public int SessionWarningBeforeTimeoutMinutes { get; set; }
    public int SessionRememberMeDurationDays { get; set; }
    public bool SessionSingleDeviceModeEnabled { get; set; }
    public int PasswordMinLength { get; set; }
    public bool PasswordRequireUppercase { get; set; }
    public bool PasswordRequireLowercase { get; set; }
    public bool PasswordRequireDigit { get; set; }
    public bool PasswordRequireNonAlphanumeric { get; set; }
    public bool MfaFeatureEnabled { get; set; }
    public bool MfaEnforcedForAll { get; set; }
    public int MfaTrustedDeviceDurationDays { get; set; }
    public string MfaBypassIpWhitelist { get; set; } = string.Empty;
    public bool RegistrationInvitationRequired { get; set; }
    public string RegistrationAllowedEmailDomains { get; set; } = string.Empty;
    public string RegistrationBlockedEmailDomains { get; set; } = string.Empty;
    public bool RegistrationAutoApproveEnabled { get; set; }
    public int AccountInactiveLockDays { get; set; }
    public bool AccountForcePasswordChangeOnFirstLogin { get; set; }
    public int AccountPasswordExpiryDays { get; set; }
    public bool LoginLockoutEnabled { get; set; }
    public int LoginMaxFailedAttempts { get; set; }
    public int LoginLockoutDurationMinutes { get; set; }
    public bool MaintenanceModeEnabled { get; set; }
    public DateTime? MaintenanceEndTime { get; set; }
    public string MaintenanceMessage { get; set; } = string.Empty;
    public bool MaintenanceCountdownEnabled { get; set; }
    public string MaintenanceIpWhitelist { get; set; } = string.Empty;
    public string MaintenanceRoleWhitelist { get; set; } = string.Empty;
    public string EmailSenderName { get; set; } = string.Empty;
    public string EmailSenderAddress { get; set; } = string.Empty;
    public int EmailDailySendLimit { get; set; }
    public int EmailRetryMaxAttempts { get; set; }
    public bool NotificationLoginAlertEnabled { get; set; }
    public string NotificationSecurityEmailRecipients { get; set; } = string.Empty;
    public string LocalizationDefaultCulture { get; set; } = string.Empty;
    public string LocalizationFallbackCulture { get; set; } = string.Empty;
    public bool LocalizationRequireUserCultureSelection { get; set; }
    public int AuditRetentionDays { get; set; }
    public int AuditCleanupScheduleHourUtc { get; set; }
    public bool AuditPiiMaskingEnabled { get; set; }
    public bool AuditLogReadOperationsEnabled { get; set; }
    public bool AuditLogCreateOperationsEnabled { get; set; }
    public bool AuditLogUpdateOperationsEnabled { get; set; }
    public bool AuditLogDeleteOperationsEnabled { get; set; }
    public bool AuditLogOtherOperationsEnabled { get; set; }
    public bool AuditLogErrorEventsEnabled { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string? ApplicationLogoUrl { get; set; }
    public string? ApplicationFaviconUrl { get; set; }
}

public class UpdateAuthSystemSettingsRequest
{
    public bool LoginEnabled { get; set; }
    public bool RegisterEnabled { get; set; }
    public bool GoogleLoginEnabled { get; set; }
    public bool ForgotPasswordEnabled { get; set; }
    public bool ChangePasswordEnabled { get; set; }
    public bool SessionManagementEnabled { get; set; }
    public bool EmailVerificationRequired { get; set; }
    public int EmailVerificationCodeExpirySeconds { get; set; }
    public int EmailVerificationResendCooldownSeconds { get; set; }
    public int SessionAccessTokenLifetimeMinutes { get; set; }
    public int SessionRefreshTokenLifetimeDays { get; set; }
    public int SessionMaxActiveSessions { get; set; }
    public int SessionIdleTimeoutMinutes { get; set; }
    public int SessionWarningBeforeTimeoutMinutes { get; set; }
    public int SessionRememberMeDurationDays { get; set; }
    public bool SessionSingleDeviceModeEnabled { get; set; }
    public int PasswordMinLength { get; set; }
    public bool PasswordRequireUppercase { get; set; }
    public bool PasswordRequireLowercase { get; set; }
    public bool PasswordRequireDigit { get; set; }
    public bool PasswordRequireNonAlphanumeric { get; set; }
    public bool MfaFeatureEnabled { get; set; }
    public bool MfaEnforcedForAll { get; set; }
    public int MfaTrustedDeviceDurationDays { get; set; }
    public string MfaBypassIpWhitelist { get; set; } = string.Empty;
    public bool RegistrationInvitationRequired { get; set; }
    public string RegistrationAllowedEmailDomains { get; set; } = string.Empty;
    public string RegistrationBlockedEmailDomains { get; set; } = string.Empty;
    public bool RegistrationAutoApproveEnabled { get; set; }
    public int AccountInactiveLockDays { get; set; }
    public bool AccountForcePasswordChangeOnFirstLogin { get; set; }
    public int AccountPasswordExpiryDays { get; set; }
    public bool LoginLockoutEnabled { get; set; }
    public int LoginMaxFailedAttempts { get; set; }
    public int LoginLockoutDurationMinutes { get; set; }
    public bool MaintenanceModeEnabled { get; set; }
    public DateTime? MaintenanceEndTime { get; set; }
    public string MaintenanceMessage { get; set; } = string.Empty;
    public bool MaintenanceCountdownEnabled { get; set; }
    public string MaintenanceIpWhitelist { get; set; } = string.Empty;
    public string MaintenanceRoleWhitelist { get; set; } = string.Empty;
    public string EmailSenderName { get; set; } = string.Empty;
    public string EmailSenderAddress { get; set; } = string.Empty;
    public int EmailDailySendLimit { get; set; }
    public int EmailRetryMaxAttempts { get; set; }
    public bool NotificationLoginAlertEnabled { get; set; }
    public string NotificationSecurityEmailRecipients { get; set; } = string.Empty;
    public string LocalizationDefaultCulture { get; set; } = string.Empty;
    public string LocalizationFallbackCulture { get; set; } = string.Empty;
    public bool LocalizationRequireUserCultureSelection { get; set; }
    public int AuditRetentionDays { get; set; }
    public int AuditCleanupScheduleHourUtc { get; set; }
    public bool AuditPiiMaskingEnabled { get; set; }
    public bool AuditLogReadOperationsEnabled { get; set; }
    public bool AuditLogCreateOperationsEnabled { get; set; }
    public bool AuditLogUpdateOperationsEnabled { get; set; }
    public bool AuditLogDeleteOperationsEnabled { get; set; }
    public bool AuditLogOtherOperationsEnabled { get; set; }
    public bool AuditLogErrorEventsEnabled { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
}

public class BrandingAssetUploadResultDto
{
    public string Url { get; set; } = string.Empty;
}
