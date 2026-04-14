export type AuthSystemSettings = {
    loginEnabled: boolean
    registerEnabled: boolean
    googleLoginEnabled: boolean
    forgotPasswordEnabled: boolean
    changePasswordEnabled: boolean
    sessionManagementEnabled: boolean
    emailVerificationRequired: boolean
    emailVerificationCodeExpirySeconds: number
    emailVerificationResendCooldownSeconds: number
    sessionAccessTokenLifetimeMinutes?: number
    sessionRefreshTokenLifetimeDays?: number
    sessionMaxActiveSessions?: number
    sessionIdleTimeoutMinutes?: number
    sessionWarningBeforeTimeoutMinutes?: number
    sessionRememberMeDurationDays?: number
    sessionSingleDeviceModeEnabled?: boolean
    passwordMinLength?: number
    passwordRequireUppercase?: boolean
    passwordRequireLowercase?: boolean
    passwordRequireDigit?: boolean
    passwordRequireNonAlphanumeric?: boolean
    mfaFeatureEnabled?: boolean
    mfaEnforcedForAll?: boolean
    mfaTrustedDeviceDurationDays?: number
    mfaBypassIpWhitelist?: string
    registrationInvitationRequired?: boolean
    registrationAllowedEmailDomains?: string
    registrationBlockedEmailDomains?: string
    registrationAutoApproveEnabled?: boolean
    accountInactiveLockDays?: number
    accountForcePasswordChangeOnFirstLogin?: boolean
    accountPasswordExpiryDays?: number
    loginLockoutEnabled?: boolean
    loginMaxFailedAttempts?: number
    loginLockoutDurationMinutes?: number
    maintenanceModeEnabled: boolean
    maintenanceEndTime?: string | null
    maintenanceMessage?: string
    maintenanceCountdownEnabled?: boolean
    maintenanceIpWhitelist?: string
    maintenanceRoleWhitelist?: string
    emailSenderName?: string
    emailSenderAddress?: string
    emailDailySendLimit?: number
    emailRetryMaxAttempts?: number
    notificationLoginAlertEnabled?: boolean
    notificationSecurityEmailRecipients?: string
    localizationDefaultCulture?: string
    localizationFallbackCulture?: string
    localizationRequireUserCultureSelection?: boolean
    auditRetentionDays?: number
    auditCleanupScheduleHourUtc?: number
    auditPiiMaskingEnabled?: boolean
    auditLogReadOperationsEnabled?: boolean
    auditLogCreateOperationsEnabled?: boolean
    auditLogUpdateOperationsEnabled?: boolean
    auditLogDeleteOperationsEnabled?: boolean
    auditLogOtherOperationsEnabled?: boolean
    auditLogErrorEventsEnabled?: boolean
    applicationName?: string
    applicationLogoUrl?: string | null
    applicationFaviconUrl?: string | null
}

export type UpdateAuthSystemSettingsRequest = AuthSystemSettings

export type BrandingAssetUploadResult = {
    url: string
}

export type ApiResponse<T> = {
    success: boolean
    message: string
    data: T
}
