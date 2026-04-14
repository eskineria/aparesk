using Eskineria.Core.Auth.Abstractions;
using Eskineria.Core.Shared.Configuration;
using Eskineria.Core.Shared.Localization;
using Eskineria.Core.Settings.Abstractions;
using Eskineria.Core.Settings.Models;
using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Repository.Specification;
using Eskineria.Core.Storage.Abstractions;
using Eskineria.Core.Shared.Response;
using Eskineria.Core.Settings.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Eskineria.Core.Settings.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private const string PiiMaskingFeatureFlagSettingName = "FeatureFlags.Security.PiiMasking";
    private const int DefaultEmailVerificationCodeExpirySeconds = 180;
    private const int MinEmailVerificationCodeExpirySeconds = 30;
    private const int MaxEmailVerificationCodeExpirySeconds = 1800;
    private const int DefaultEmailVerificationResendCooldownSeconds = 60;
    private const int MinEmailVerificationResendCooldownSeconds = 5;
    private const int MaxEmailVerificationResendCooldownSeconds = 600;
    private const int DefaultSessionAccessTokenLifetimeMinutes = 60;
    private const int MinSessionAccessTokenLifetimeMinutes = 5;
    private const int MaxSessionAccessTokenLifetimeMinutes = 240;
    private const int DefaultSessionRefreshTokenLifetimeDays = 7;
    private const int MinSessionRefreshTokenLifetimeDays = 1;
    private const int MaxSessionRefreshTokenLifetimeDays = 90;
    private const int DefaultSessionMaxActiveSessions = 5;
    private const int MinSessionMaxActiveSessions = 1;
    private const int MaxSessionMaxActiveSessions = 20;
    private const int DefaultSessionIdleTimeoutMinutes = 60;
    private const int MinSessionIdleTimeoutMinutes = 5;
    private const int MaxSessionIdleTimeoutMinutes = 1440;
    private const int DefaultSessionWarningBeforeTimeoutMinutes = 2;
    private const int MinSessionWarningBeforeTimeoutMinutes = 1;
    private const int MaxSessionWarningBeforeTimeoutMinutes = 120;
    private const int DefaultSessionRememberMeDurationDays = 30;
    private const int MinSessionRememberMeDurationDays = 1;
    private const int MaxSessionRememberMeDurationDays = 365;
    private const int DefaultPasswordMinLength = 8;
    private const int MinPasswordMinLength = 6;
    private const int MaxPasswordMinLength = 128;
    private const int DefaultLoginMaxFailedAttempts = 5;
    private const int MinLoginMaxFailedAttempts = 3;
    private const int MaxLoginMaxFailedAttempts = 20;
    private const int DefaultLoginLockoutDurationMinutes = 15;
    private const int MinLoginLockoutDurationMinutes = 1;
    private const int MaxLoginLockoutDurationMinutes = 1440;
    private const int DefaultMfaTrustedDeviceDurationDays = 30;
    private const int MinMfaTrustedDeviceDurationDays = 0;
    private const int MaxMfaTrustedDeviceDurationDays = 365;
    private const int DefaultAccountInactiveLockDays = 90;
    private const int MinAccountInactiveLockDays = 7;
    private const int MaxAccountInactiveLockDays = 3650;
    private const int DefaultAccountPasswordExpiryDays = 90;
    private const int MinAccountPasswordExpiryDays = 0;
    private const int MaxAccountPasswordExpiryDays = 3650;
    private const int DefaultEmailDailySendLimit = 5000;
    private const int MinEmailDailySendLimit = 0;
    private const int MaxEmailDailySendLimit = 200000;
    private const int DefaultEmailRetryMaxAttempts = 3;
    private const int MinEmailRetryMaxAttempts = 0;
    private const int MaxEmailRetryMaxAttempts = 20;
    private const int DefaultAuditRetentionDays = 365;
    private const int MinAuditRetentionDays = 30;
    private const int MaxAuditRetentionDays = 3650;
    private const int DefaultAuditCleanupScheduleHourUtc = 2;
    private const int MinAuditCleanupScheduleHourUtc = 0;
    private const int MaxAuditCleanupScheduleHourUtc = 23;
    private const int MaxBrandingAssetSizeBytes = 2 * 1024 * 1024;
    private const int MaxApplicationNameLength = 120;
    private const int MaxLargeTextSettingLength = 4000;
    private const int MaxMediumTextSettingLength = 1000;
    private const string BrandingFolderName = "branding";
    private const string DefaultApplicationName = "Eskineria Backend";
    private const string DefaultLocalizationCulture = "en-US";
    private static readonly Regex UnsafeControlCharsRegex = new(@"[\r\n\t]+", RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
    };

    private static readonly HashSet<string> AllowedFaviconExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".ico",
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedBrandingContentTypes =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/png"
            },
            [".jpg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/pjpeg"
            },
            [".jpeg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/pjpeg"
            },
            [".ico"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/x-icon",
                "image/vnd.microsoft.icon"
            }
        };

    private static readonly IReadOnlyDictionary<string, string> AuthSettingDefaults =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemSettingKeys.AuthLoginEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthRegisterEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthGoogleLoginEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthForgotPasswordEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthChangePasswordEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthSessionManagementEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthEmailVerificationRequired] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthEmailVerificationCodeExpirySeconds] = DefaultEmailVerificationCodeExpirySeconds.ToString(),
            [SystemSettingKeys.AuthEmailVerificationResendCooldownSeconds] = DefaultEmailVerificationResendCooldownSeconds.ToString(),
            [SystemSettingKeys.AuthSessionAccessTokenLifetimeMinutes] = DefaultSessionAccessTokenLifetimeMinutes.ToString(),
            [SystemSettingKeys.AuthSessionRefreshTokenLifetimeDays] = DefaultSessionRefreshTokenLifetimeDays.ToString(),
            [SystemSettingKeys.AuthSessionMaxActiveSessions] = DefaultSessionMaxActiveSessions.ToString(),
            [SystemSettingKeys.AuthSessionIdleTimeoutMinutes] = DefaultSessionIdleTimeoutMinutes.ToString(),
            [SystemSettingKeys.AuthSessionWarningBeforeTimeoutMinutes] = DefaultSessionWarningBeforeTimeoutMinutes.ToString(),
            [SystemSettingKeys.AuthSessionRememberMeDurationDays] = DefaultSessionRememberMeDurationDays.ToString(),
            [SystemSettingKeys.AuthSessionSingleDeviceModeEnabled] = bool.FalseString.ToLowerInvariant(),
            [SystemSettingKeys.AuthPasswordPolicyMinLength] = DefaultPasswordMinLength.ToString(),
            [SystemSettingKeys.AuthPasswordPolicyRequireUppercase] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthPasswordPolicyRequireLowercase] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthPasswordPolicyRequireDigit] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthPasswordPolicyRequireNonAlphanumeric] = bool.FalseString.ToLowerInvariant(),
            [SystemSettingKeys.AuthMfaEnforceForAdmins] = bool.FalseString.ToLowerInvariant(),
            [SystemSettingKeys.AuthMfaTrustedDeviceDurationDays] = DefaultMfaTrustedDeviceDurationDays.ToString(),
            [SystemSettingKeys.AuthMfaBypassIpWhitelist] = string.Empty,
            [SystemSettingKeys.AuthRegistrationInvitationRequired] = bool.FalseString.ToLowerInvariant(),
            [SystemSettingKeys.AuthRegistrationAllowedEmailDomains] = string.Empty,
            [SystemSettingKeys.AuthRegistrationBlockedEmailDomains] = string.Empty,
            [SystemSettingKeys.AuthRegistrationAutoApproveEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthAccountLifecycleInactiveLockDays] = DefaultAccountInactiveLockDays.ToString(),
            [SystemSettingKeys.AuthAccountLifecycleForcePasswordChangeOnFirstLogin] = bool.FalseString.ToLowerInvariant(),
            [SystemSettingKeys.AuthAccountLifecyclePasswordExpiryDays] = DefaultAccountPasswordExpiryDays.ToString(),
            [SystemSettingKeys.AuthLoginSecurityLockoutEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.AuthLoginSecurityMaxFailedAttempts] = DefaultLoginMaxFailedAttempts.ToString(),
            [SystemSettingKeys.AuthLoginSecurityLockoutDurationMinutes] = DefaultLoginLockoutDurationMinutes.ToString(),
            [SystemSettingKeys.SystemMaintenanceModeEnabled] = bool.FalseString.ToLowerInvariant(),
            [SystemSettingKeys.SystemMaintenanceEndTime] = string.Empty,
            [SystemSettingKeys.SystemMaintenanceMessage] = string.Empty,
            [SystemSettingKeys.SystemMaintenanceCountdownEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.SystemMaintenanceIpWhitelist] = string.Empty,
            [SystemSettingKeys.SystemMaintenanceRoleWhitelist] = "Admin",
            [SystemSettingKeys.SystemBrandingApplicationName] = DefaultApplicationName,
            [SystemSettingKeys.SystemBrandingApplicationLogoPath] = string.Empty,
            [SystemSettingKeys.SystemBrandingApplicationFaviconPath] = string.Empty,
            [SystemSettingKeys.SystemEmailSenderName] = "Eskineria",
            [SystemSettingKeys.SystemEmailSenderAddress] = "no-reply@eskineria.local",
            [SystemSettingKeys.SystemEmailDailySendLimit] = DefaultEmailDailySendLimit.ToString(),
            [SystemSettingKeys.SystemEmailRetryMaxAttempts] = DefaultEmailRetryMaxAttempts.ToString(),
            [SystemSettingKeys.SystemNotificationsLoginAlertEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.SystemNotificationsSecurityEmailRecipients] = string.Empty,
            [SystemSettingKeys.SystemLocalizationDefaultCulture] = DefaultLocalizationCulture,
            [SystemSettingKeys.SystemLocalizationFallbackCulture] = DefaultLocalizationCulture,
            [SystemSettingKeys.SystemLocalizationRequireUserCultureSelection] = bool.FalseString.ToLowerInvariant(),
            [SystemSettingKeys.SystemAuditRetentionDays] = DefaultAuditRetentionDays.ToString(),
            [SystemSettingKeys.SystemAuditCleanupScheduleHourUtc] = DefaultAuditCleanupScheduleHourUtc.ToString(),
            [SystemSettingKeys.SystemAuditPiiMaskingEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.SystemAuditLogReadOperationsEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.SystemAuditLogCreateOperationsEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.SystemAuditLogUpdateOperationsEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.SystemAuditLogDeleteOperationsEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.SystemAuditLogOtherOperationsEnabled] = bool.TrueString.ToLowerInvariant(),
            [SystemSettingKeys.SystemAuditLogErrorEventsEnabled] = bool.TrueString.ToLowerInvariant(),
            [PiiMaskingFeatureFlagSettingName] = bool.TrueString.ToLowerInvariant(),
        };

    private readonly ISettingRepository _settingRepository;
    private readonly IStorageService _storageService;
    private readonly IAuditLoggingPolicyCacheInvalidator _auditLoggingPolicyCacheInvalidator;
    private readonly IStringLocalizer<SystemSettingsService> _localizer;

    public SystemSettingsService(
        ISettingRepository settingRepository,
        IStorageService storageService,
        IAuditLoggingPolicyCacheInvalidator auditLoggingPolicyCacheInvalidator,
        IStringLocalizer<SystemSettingsService> localizer)
    {
        _settingRepository = settingRepository;
        _storageService = storageService;
        _auditLoggingPolicyCacheInvalidator = auditLoggingPolicyCacheInvalidator;
        _localizer = localizer;
    }

    public async Task<DataResponse<AuthSystemSettingsDto>> GetAuthSettingsAsync()
    {
        var settingsMap = await GetAuthSettingsMapAsync();

        var dto = new AuthSystemSettingsDto
        {
            LoginEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthLoginEnabled),
            RegisterEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthRegisterEnabled),
            GoogleLoginEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthGoogleLoginEnabled),
            ForgotPasswordEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthForgotPasswordEnabled),
            ChangePasswordEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthChangePasswordEnabled),
            SessionManagementEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthSessionManagementEnabled),
            EmailVerificationRequired = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthEmailVerificationRequired),
            EmailVerificationCodeExpirySeconds = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthEmailVerificationCodeExpirySeconds,
                DefaultEmailVerificationCodeExpirySeconds,
                NormalizeEmailVerificationCodeExpirySeconds),
            EmailVerificationResendCooldownSeconds = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthEmailVerificationResendCooldownSeconds,
                DefaultEmailVerificationResendCooldownSeconds,
                NormalizeEmailVerificationResendCooldownSeconds),
            SessionAccessTokenLifetimeMinutes = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthSessionAccessTokenLifetimeMinutes,
                DefaultSessionAccessTokenLifetimeMinutes,
                NormalizeSessionAccessTokenLifetimeMinutes),
            SessionRefreshTokenLifetimeDays = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthSessionRefreshTokenLifetimeDays,
                DefaultSessionRefreshTokenLifetimeDays,
                NormalizeSessionRefreshTokenLifetimeDays),
            SessionMaxActiveSessions = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthSessionMaxActiveSessions,
                DefaultSessionMaxActiveSessions,
                NormalizeSessionMaxActiveSessions),
            SessionIdleTimeoutMinutes = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthSessionIdleTimeoutMinutes,
                DefaultSessionIdleTimeoutMinutes,
                NormalizeSessionIdleTimeoutMinutes),
            SessionWarningBeforeTimeoutMinutes = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthSessionWarningBeforeTimeoutMinutes,
                DefaultSessionWarningBeforeTimeoutMinutes,
                NormalizeSessionWarningBeforeTimeoutMinutes),
            SessionRememberMeDurationDays = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthSessionRememberMeDurationDays,
                DefaultSessionRememberMeDurationDays,
                NormalizeSessionRememberMeDurationDays),
            SessionSingleDeviceModeEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthSessionSingleDeviceModeEnabled),
            PasswordMinLength = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthPasswordPolicyMinLength,
                DefaultPasswordMinLength,
                NormalizePasswordMinLength),
            PasswordRequireUppercase = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthPasswordPolicyRequireUppercase),
            PasswordRequireLowercase = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthPasswordPolicyRequireLowercase),
            PasswordRequireDigit = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthPasswordPolicyRequireDigit),
            PasswordRequireNonAlphanumeric = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthPasswordPolicyRequireNonAlphanumeric),
            MfaEnforceForAdmins = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthMfaEnforceForAdmins),
            MfaTrustedDeviceDurationDays = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthMfaTrustedDeviceDurationDays,
                DefaultMfaTrustedDeviceDurationDays,
                NormalizeMfaTrustedDeviceDurationDays),
            MfaBypassIpWhitelist = NormalizeTextSetting(
                GetStringSettingValue(settingsMap, SystemSettingKeys.AuthMfaBypassIpWhitelist, string.Empty),
                MaxMediumTextSettingLength),
            RegistrationInvitationRequired = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthRegistrationInvitationRequired),
            RegistrationAllowedEmailDomains = NormalizeTextSetting(
                GetStringSettingValue(settingsMap, SystemSettingKeys.AuthRegistrationAllowedEmailDomains, string.Empty),
                MaxMediumTextSettingLength),
            RegistrationBlockedEmailDomains = NormalizeTextSetting(
                GetStringSettingValue(settingsMap, SystemSettingKeys.AuthRegistrationBlockedEmailDomains, string.Empty),
                MaxMediumTextSettingLength),
            RegistrationAutoApproveEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthRegistrationAutoApproveEnabled),
            AccountInactiveLockDays = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthAccountLifecycleInactiveLockDays,
                DefaultAccountInactiveLockDays,
                NormalizeAccountInactiveLockDays),
            AccountForcePasswordChangeOnFirstLogin = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthAccountLifecycleForcePasswordChangeOnFirstLogin),
            AccountPasswordExpiryDays = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthAccountLifecyclePasswordExpiryDays,
                DefaultAccountPasswordExpiryDays,
                NormalizeAccountPasswordExpiryDays),
            LoginLockoutEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.AuthLoginSecurityLockoutEnabled),
            LoginMaxFailedAttempts = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthLoginSecurityMaxFailedAttempts,
                DefaultLoginMaxFailedAttempts,
                NormalizeLoginMaxFailedAttempts),
            LoginLockoutDurationMinutes = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.AuthLoginSecurityLockoutDurationMinutes,
                DefaultLoginLockoutDurationMinutes,
                NormalizeLoginLockoutDurationMinutes),
            MaintenanceModeEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemMaintenanceModeEnabled),
            MaintenanceEndTime = GetDateTimeSettingValue(settingsMap, SystemSettingKeys.SystemMaintenanceEndTime),
            MaintenanceMessage = NormalizeTextSetting(
                GetStringSettingValue(settingsMap, SystemSettingKeys.SystemMaintenanceMessage, string.Empty),
                MaxLargeTextSettingLength),
            MaintenanceCountdownEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemMaintenanceCountdownEnabled),
            MaintenanceIpWhitelist = NormalizeTextSetting(
                GetStringSettingValue(settingsMap, SystemSettingKeys.SystemMaintenanceIpWhitelist, string.Empty),
                MaxMediumTextSettingLength),
            MaintenanceRoleWhitelist = NormalizeTextSetting(
                GetStringSettingValue(settingsMap, SystemSettingKeys.SystemMaintenanceRoleWhitelist, "Admin"),
                MaxMediumTextSettingLength),
            EmailSenderName = NormalizeTextSetting(
                GetStringSettingValue(settingsMap, SystemSettingKeys.SystemEmailSenderName, "Eskineria"),
                200),
            EmailSenderAddress = NormalizeTextSetting(
                GetStringSettingValue(settingsMap, SystemSettingKeys.SystemEmailSenderAddress, "no-reply@eskineria.local"),
                200),
            EmailDailySendLimit = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.SystemEmailDailySendLimit,
                DefaultEmailDailySendLimit,
                NormalizeEmailDailySendLimit),
            EmailRetryMaxAttempts = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.SystemEmailRetryMaxAttempts,
                DefaultEmailRetryMaxAttempts,
                NormalizeEmailRetryMaxAttempts),
            NotificationLoginAlertEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemNotificationsLoginAlertEnabled),
            NotificationSecurityEmailRecipients = NormalizeTextSetting(
                GetStringSettingValue(settingsMap, SystemSettingKeys.SystemNotificationsSecurityEmailRecipients, string.Empty),
                MaxMediumTextSettingLength),
            LocalizationDefaultCulture = NormalizeCultureCode(
                GetStringSettingValue(settingsMap, SystemSettingKeys.SystemLocalizationDefaultCulture, DefaultLocalizationCulture)),
            LocalizationFallbackCulture = NormalizeCultureCode(
                GetStringSettingValue(settingsMap, SystemSettingKeys.SystemLocalizationFallbackCulture, DefaultLocalizationCulture)),
            LocalizationRequireUserCultureSelection = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemLocalizationRequireUserCultureSelection),
            AuditRetentionDays = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.SystemAuditRetentionDays,
                DefaultAuditRetentionDays,
                NormalizeAuditRetentionDays),
            AuditCleanupScheduleHourUtc = GetIntSettingValue(
                settingsMap,
                SystemSettingKeys.SystemAuditCleanupScheduleHourUtc,
                DefaultAuditCleanupScheduleHourUtc,
                NormalizeAuditCleanupScheduleHourUtc),
            AuditPiiMaskingEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemAuditPiiMaskingEnabled),
            AuditLogReadOperationsEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemAuditLogReadOperationsEnabled),
            AuditLogCreateOperationsEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemAuditLogCreateOperationsEnabled),
            AuditLogUpdateOperationsEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemAuditLogUpdateOperationsEnabled),
            AuditLogDeleteOperationsEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemAuditLogDeleteOperationsEnabled),
            AuditLogOtherOperationsEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemAuditLogOtherOperationsEnabled),
            AuditLogErrorEventsEnabled = GetBooleanSettingValue(settingsMap, SystemSettingKeys.SystemAuditLogErrorEventsEnabled),
            ApplicationName = NormalizeApplicationName(
                GetStringSettingValue(
                    settingsMap,
                    SystemSettingKeys.SystemBrandingApplicationName,
                    DefaultApplicationName)),
            ApplicationLogoUrl = BuildPublicAssetUrl(
                GetStringSettingValue(
                    settingsMap,
                    SystemSettingKeys.SystemBrandingApplicationLogoPath,
                    string.Empty)),
            ApplicationFaviconUrl = BuildPublicAssetUrl(
                GetStringSettingValue(
                    settingsMap,
                    SystemSettingKeys.SystemBrandingApplicationFaviconPath,
                    string.Empty)),
        };

        return DataResponse<AuthSystemSettingsDto>.Succeed(
            dto,
            _localizer[LocalizationKeys.SystemSettingsRetrievedSuccessfully]);
    }

    public async Task<Response> UpdateAuthSettingsAsync(UpdateAuthSystemSettingsRequest request)
    {
        if (request == null)
        {
            return Response.Fail("Invalid system settings payload.", 400);
        }

        var expirySeconds = NormalizeEmailVerificationCodeExpirySeconds(request.EmailVerificationCodeExpirySeconds);
        var resendCooldownSeconds = NormalizeEmailVerificationResendCooldownSeconds(request.EmailVerificationResendCooldownSeconds);
        var sessionAccessTokenLifetimeMinutes = NormalizeSessionAccessTokenLifetimeMinutes(request.SessionAccessTokenLifetimeMinutes);
        var sessionRefreshTokenLifetimeDays = NormalizeSessionRefreshTokenLifetimeDays(request.SessionRefreshTokenLifetimeDays);
        var sessionMaxActiveSessions = NormalizeSessionMaxActiveSessions(request.SessionMaxActiveSessions);
        var sessionIdleTimeoutMinutes = NormalizeSessionIdleTimeoutMinutes(request.SessionIdleTimeoutMinutes);
        var sessionWarningBeforeTimeoutMinutes = NormalizeSessionWarningBeforeTimeoutMinutes(request.SessionWarningBeforeTimeoutMinutes);
        var sessionRememberMeDurationDays = NormalizeSessionRememberMeDurationDays(request.SessionRememberMeDurationDays);
        var passwordMinLength = NormalizePasswordMinLength(request.PasswordMinLength);
        var mfaTrustedDeviceDurationDays = NormalizeMfaTrustedDeviceDurationDays(request.MfaTrustedDeviceDurationDays);
        var accountInactiveLockDays = NormalizeAccountInactiveLockDays(request.AccountInactiveLockDays);
        var accountPasswordExpiryDays = NormalizeAccountPasswordExpiryDays(request.AccountPasswordExpiryDays);
        var loginMaxFailedAttempts = NormalizeLoginMaxFailedAttempts(request.LoginMaxFailedAttempts);
        var loginLockoutDurationMinutes = NormalizeLoginLockoutDurationMinutes(request.LoginLockoutDurationMinutes);
        var emailDailySendLimit = NormalizeEmailDailySendLimit(request.EmailDailySendLimit);
        var emailRetryMaxAttempts = NormalizeEmailRetryMaxAttempts(request.EmailRetryMaxAttempts);
        var auditRetentionDays = NormalizeAuditRetentionDays(request.AuditRetentionDays);
        var auditCleanupScheduleHourUtc = NormalizeAuditCleanupScheduleHourUtc(request.AuditCleanupScheduleHourUtc);
        var applicationName = NormalizeApplicationName(request.ApplicationName);
        var mfaBypassIpWhitelist = NormalizeTextSetting(request.MfaBypassIpWhitelist, MaxMediumTextSettingLength);
        var registrationAllowedEmailDomains = NormalizeTextSetting(request.RegistrationAllowedEmailDomains, MaxMediumTextSettingLength);
        var registrationBlockedEmailDomains = NormalizeTextSetting(request.RegistrationBlockedEmailDomains, MaxMediumTextSettingLength);
        var maintenanceMessage = NormalizeTextSetting(request.MaintenanceMessage, MaxLargeTextSettingLength);
        var maintenanceIpWhitelist = NormalizeTextSetting(request.MaintenanceIpWhitelist, MaxMediumTextSettingLength);
        var maintenanceRoleWhitelist = NormalizeTextSetting(request.MaintenanceRoleWhitelist, MaxMediumTextSettingLength);
        var emailSenderName = NormalizeTextSetting(request.EmailSenderName, 200);
        var emailSenderAddress = NormalizeEmailAddress(
            NormalizeTextSetting(request.EmailSenderAddress, 200));
        var notificationSecurityEmailRecipients = NormalizeTextSetting(request.NotificationSecurityEmailRecipients, MaxMediumTextSettingLength);
        var localizationDefaultCulture = NormalizeCultureCode(request.LocalizationDefaultCulture);
        var localizationFallbackCulture = NormalizeCultureCode(request.LocalizationFallbackCulture);
        var targetSettingNames = AuthSettingDefaults.Keys.ToList();

        var existingSettings = await SpecificationEvaluator<Setting>.GetQuery(
                _settingRepository.Query(asNoTracking: false),
                new QuerySpecification<Setting>(s => targetSettingNames.Contains(s.Name)))
            .ToListAsync();

        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthLoginEnabled, request.LoginEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthRegisterEnabled, request.RegisterEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthGoogleLoginEnabled, request.GoogleLoginEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthForgotPasswordEnabled, request.ForgotPasswordEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthChangePasswordEnabled, request.ChangePasswordEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthSessionManagementEnabled, request.SessionManagementEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthEmailVerificationRequired, request.EmailVerificationRequired);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthEmailVerificationCodeExpirySeconds, expirySeconds);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthEmailVerificationResendCooldownSeconds, resendCooldownSeconds);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthSessionAccessTokenLifetimeMinutes, sessionAccessTokenLifetimeMinutes);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthSessionRefreshTokenLifetimeDays, sessionRefreshTokenLifetimeDays);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthSessionMaxActiveSessions, sessionMaxActiveSessions);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthSessionIdleTimeoutMinutes, sessionIdleTimeoutMinutes);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthSessionWarningBeforeTimeoutMinutes, sessionWarningBeforeTimeoutMinutes);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthSessionRememberMeDurationDays, sessionRememberMeDurationDays);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthSessionSingleDeviceModeEnabled, request.SessionSingleDeviceModeEnabled);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthPasswordPolicyMinLength, passwordMinLength);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthPasswordPolicyRequireUppercase, request.PasswordRequireUppercase);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthPasswordPolicyRequireLowercase, request.PasswordRequireLowercase);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthPasswordPolicyRequireDigit, request.PasswordRequireDigit);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthPasswordPolicyRequireNonAlphanumeric, request.PasswordRequireNonAlphanumeric);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthMfaEnforceForAdmins, request.MfaEnforceForAdmins);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthMfaTrustedDeviceDurationDays, mfaTrustedDeviceDurationDays);
        UpsertStringSetting(existingSettings, SystemSettingKeys.AuthMfaBypassIpWhitelist, mfaBypassIpWhitelist);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthRegistrationInvitationRequired, request.RegistrationInvitationRequired);
        UpsertStringSetting(existingSettings, SystemSettingKeys.AuthRegistrationAllowedEmailDomains, registrationAllowedEmailDomains);
        UpsertStringSetting(existingSettings, SystemSettingKeys.AuthRegistrationBlockedEmailDomains, registrationBlockedEmailDomains);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthRegistrationAutoApproveEnabled, request.RegistrationAutoApproveEnabled);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthAccountLifecycleInactiveLockDays, accountInactiveLockDays);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthAccountLifecycleForcePasswordChangeOnFirstLogin, request.AccountForcePasswordChangeOnFirstLogin);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthAccountLifecyclePasswordExpiryDays, accountPasswordExpiryDays);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.AuthLoginSecurityLockoutEnabled, request.LoginLockoutEnabled);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthLoginSecurityMaxFailedAttempts, loginMaxFailedAttempts);
        UpsertIntSetting(existingSettings, SystemSettingKeys.AuthLoginSecurityLockoutDurationMinutes, loginLockoutDurationMinutes);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemMaintenanceModeEnabled, request.MaintenanceModeEnabled);
        UpsertDateTimeSetting(existingSettings, SystemSettingKeys.SystemMaintenanceEndTime, request.MaintenanceEndTime);
        UpsertStringSetting(existingSettings, SystemSettingKeys.SystemMaintenanceMessage, maintenanceMessage);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemMaintenanceCountdownEnabled, request.MaintenanceCountdownEnabled);
        UpsertStringSetting(existingSettings, SystemSettingKeys.SystemMaintenanceIpWhitelist, maintenanceIpWhitelist);
        UpsertStringSetting(existingSettings, SystemSettingKeys.SystemMaintenanceRoleWhitelist, maintenanceRoleWhitelist);
        UpsertStringSetting(existingSettings, SystemSettingKeys.SystemEmailSenderName, emailSenderName);
        UpsertStringSetting(existingSettings, SystemSettingKeys.SystemEmailSenderAddress, emailSenderAddress);
        UpsertIntSetting(existingSettings, SystemSettingKeys.SystemEmailDailySendLimit, emailDailySendLimit);
        UpsertIntSetting(existingSettings, SystemSettingKeys.SystemEmailRetryMaxAttempts, emailRetryMaxAttempts);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemNotificationsLoginAlertEnabled, request.NotificationLoginAlertEnabled);
        UpsertStringSetting(existingSettings, SystemSettingKeys.SystemNotificationsSecurityEmailRecipients, notificationSecurityEmailRecipients);
        UpsertStringSetting(existingSettings, SystemSettingKeys.SystemLocalizationDefaultCulture, localizationDefaultCulture);
        UpsertStringSetting(existingSettings, SystemSettingKeys.SystemLocalizationFallbackCulture, localizationFallbackCulture);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemLocalizationRequireUserCultureSelection, request.LocalizationRequireUserCultureSelection);
        UpsertIntSetting(existingSettings, SystemSettingKeys.SystemAuditRetentionDays, auditRetentionDays);
        UpsertIntSetting(existingSettings, SystemSettingKeys.SystemAuditCleanupScheduleHourUtc, auditCleanupScheduleHourUtc);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemAuditPiiMaskingEnabled, request.AuditPiiMaskingEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemAuditLogReadOperationsEnabled, request.AuditLogReadOperationsEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemAuditLogCreateOperationsEnabled, request.AuditLogCreateOperationsEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemAuditLogUpdateOperationsEnabled, request.AuditLogUpdateOperationsEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemAuditLogDeleteOperationsEnabled, request.AuditLogDeleteOperationsEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemAuditLogOtherOperationsEnabled, request.AuditLogOtherOperationsEnabled);
        UpsertBooleanSetting(existingSettings, SystemSettingKeys.SystemAuditLogErrorEventsEnabled, request.AuditLogErrorEventsEnabled);
        UpsertBooleanSetting(existingSettings, PiiMaskingFeatureFlagSettingName, request.AuditPiiMaskingEnabled);
        UpsertStringSetting(existingSettings, SystemSettingKeys.SystemBrandingApplicationName, applicationName);

        var newSettings = existingSettings.Where(x => x.Id == 0).ToList();
        if (newSettings.Count > 0)
        {
            await _settingRepository.AddRangeAsync(newSettings);
        }

        await _settingRepository.SaveChangesAsync();
        _auditLoggingPolicyCacheInvalidator.Invalidate();

        return Response.Succeed(_localizer[LocalizationKeys.SystemSettingsUpdatedSuccessfully]);
    }

    public Task<DataResponse<BrandingAssetUploadResultDto>> UploadApplicationLogoAsync(IFormFile file)
    {
        return UploadBrandingAssetAsync(file, SystemSettingKeys.SystemBrandingApplicationLogoPath, AllowedLogoExtensions);
    }

    public Task<DataResponse<BrandingAssetUploadResultDto>> UploadApplicationFaviconAsync(IFormFile file)
    {
        return UploadBrandingAssetAsync(file, SystemSettingKeys.SystemBrandingApplicationFaviconPath, AllowedFaviconExtensions);
    }

    public Task<bool> IsLoginEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthLoginEnabled);
    }

    public Task<bool> IsRegistrationEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthRegisterEnabled);
    }

    public Task<bool> IsGoogleLoginEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthGoogleLoginEnabled);
    }

    public Task<bool> IsForgotPasswordEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthForgotPasswordEnabled);
    }

    public Task<bool> IsChangePasswordEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthChangePasswordEnabled);
    }

    public Task<bool> IsSessionManagementEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthSessionManagementEnabled);
    }

    public Task<bool> IsEmailVerificationRequiredAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthEmailVerificationRequired);
    }

    public Task<int> GetEmailVerificationCodeExpirySecondsAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthEmailVerificationCodeExpirySeconds,
            DefaultEmailVerificationCodeExpirySeconds,
            NormalizeEmailVerificationCodeExpirySeconds);
    }

    public Task<int> GetEmailVerificationResendCooldownSecondsAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthEmailVerificationResendCooldownSeconds,
            DefaultEmailVerificationResendCooldownSeconds,
            NormalizeEmailVerificationResendCooldownSeconds);
    }

    public Task<int> GetSessionAccessTokenLifetimeMinutesAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthSessionAccessTokenLifetimeMinutes,
            DefaultSessionAccessTokenLifetimeMinutes,
            NormalizeSessionAccessTokenLifetimeMinutes);
    }

    public Task<int> GetSessionRefreshTokenLifetimeDaysAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthSessionRefreshTokenLifetimeDays,
            DefaultSessionRefreshTokenLifetimeDays,
            NormalizeSessionRefreshTokenLifetimeDays);
    }

    public Task<int> GetSessionMaxActiveSessionsAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthSessionMaxActiveSessions,
            DefaultSessionMaxActiveSessions,
            NormalizeSessionMaxActiveSessions);
    }

    public Task<int> GetSessionIdleTimeoutMinutesAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthSessionIdleTimeoutMinutes,
            DefaultSessionIdleTimeoutMinutes,
            NormalizeSessionIdleTimeoutMinutes);
    }

    public Task<int> GetSessionWarningBeforeTimeoutMinutesAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthSessionWarningBeforeTimeoutMinutes,
            DefaultSessionWarningBeforeTimeoutMinutes,
            NormalizeSessionWarningBeforeTimeoutMinutes);
    }

    public Task<int> GetSessionRememberMeDurationDaysAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthSessionRememberMeDurationDays,
            DefaultSessionRememberMeDurationDays,
            NormalizeSessionRememberMeDurationDays);
    }

    public Task<bool> IsSessionSingleDeviceModeEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthSessionSingleDeviceModeEnabled);
    }

    public Task<int> GetPasswordMinLengthAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthPasswordPolicyMinLength,
            DefaultPasswordMinLength,
            NormalizePasswordMinLength);
    }

    public Task<bool> IsPasswordRequireUppercaseAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthPasswordPolicyRequireUppercase);
    }

    public Task<bool> IsPasswordRequireLowercaseAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthPasswordPolicyRequireLowercase);
    }

    public Task<bool> IsPasswordRequireDigitAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthPasswordPolicyRequireDigit);
    }

    public Task<bool> IsPasswordRequireNonAlphanumericAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthPasswordPolicyRequireNonAlphanumeric);
    }

    public Task<bool> IsLoginLockoutEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.AuthLoginSecurityLockoutEnabled);
    }

    public Task<int> GetLoginMaxFailedAttemptsAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthLoginSecurityMaxFailedAttempts,
            DefaultLoginMaxFailedAttempts,
            NormalizeLoginMaxFailedAttempts);
    }

    public Task<int> GetLoginLockoutDurationMinutesAsync()
    {
        return GetIntSettingValueAsync(
            SystemSettingKeys.AuthLoginSecurityLockoutDurationMinutes,
            DefaultLoginLockoutDurationMinutes,
            NormalizeLoginLockoutDurationMinutes);
    }

    public Task<bool> IsNotificationLoginAlertEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.SystemNotificationsLoginAlertEnabled);
    }

    public Task<bool> IsMaintenanceModeEnabledAsync()
    {
        return GetBooleanSettingValueAsync(SystemSettingKeys.SystemMaintenanceModeEnabled);
    }

    private async Task<bool> GetBooleanSettingValueAsync(string settingName)
    {
        var value = await GetScopedSettingValueAsync(settingName);
        return ParseBooleanSetting(value, GetDefaultBooleanValue(settingName));
    }

    private async Task<int> GetIntSettingValueAsync(string settingName, int fallback, Func<int, int> normalize)
    {
        var defaultValue = GetDefaultIntValue(settingName, fallback);
        var value = await GetScopedSettingValueAsync(settingName);
        var parsed = ParseIntSetting(value, defaultValue);
        return normalize(parsed);
    }

    private async Task<Dictionary<string, string>> GetAuthSettingsMapAsync()
    {
        var settings = await _settingRepository.GetListAsync(
            new QuerySpecification<Setting>(s => AuthSettingDefaults.Keys.Contains(s.Name)));

        await EnsureDefaultSettingsAsync(settings);

        return settings.ToDictionary(s => s.Name, s => s.Value, StringComparer.OrdinalIgnoreCase);
    }

    private async Task EnsureDefaultSettingsAsync(List<Setting> existingSettings)
    {
        var existingNames = existingSettings
            .Select(s => s.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingSettings = AuthSettingDefaults
            .Where(x => !existingNames.Contains(x.Key))
            .Select(x => new Setting
            {
                Name = x.Key,
                Value = x.Value,
            })
            .ToList();

        if (missingSettings.Count == 0)
        {
            return;
        }

        existingSettings.AddRange(missingSettings);

        try
        {
            await _settingRepository.AddRangeAsync(missingSettings);
            await _settingRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Ignore concurrent inserts for first-run defaults.
        }
    }

    private void UpsertBooleanSetting(ICollection<Setting> existingSettings, string settingName, bool value)
    {
        UpsertSetting(existingSettings, settingName, value.ToString().ToLowerInvariant());
    }

    private void UpsertIntSetting(ICollection<Setting> existingSettings, string settingName, int value)
    {
        UpsertSetting(existingSettings, settingName, value.ToString());
    }

    private void UpsertDateTimeSetting(ICollection<Setting> existingSettings, string settingName, DateTime? value)
    {
        UpsertSetting(existingSettings, settingName, value?.ToString("O") ?? string.Empty);
    }

    private void UpsertStringSetting(ICollection<Setting> existingSettings, string settingName, string value)
    {
        UpsertSetting(existingSettings, settingName, value);
    }

    private void UpsertSetting(ICollection<Setting> existingSettings, string settingName, string value)
    {
        var existingSetting = existingSettings
            .FirstOrDefault(s => string.Equals(s.Name, settingName, StringComparison.OrdinalIgnoreCase));

        if (existingSetting != null)
        {
            existingSetting.Value = value;
            return;
        }

        existingSettings.Add(new Setting
        {
            Name = settingName,
            Value = value,
        });
    }

    private async Task<DataResponse<BrandingAssetUploadResultDto>> UploadBrandingAssetAsync(
        IFormFile file,
        string settingKey,
        IReadOnlySet<string> allowedExtensions)
    {
        if (file == null || file.Length == 0)
        {
            return DataResponse<BrandingAssetUploadResultDto>.Fail("File is required.", 400);
        }

        if (file.Length > MaxBrandingAssetSizeBytes)
        {
            return DataResponse<BrandingAssetUploadResultDto>.Fail($"File size cannot exceed {MaxBrandingAssetSizeBytes} bytes.", 400);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            return DataResponse<BrandingAssetUploadResultDto>.Fail("Unsupported file type.", 400);
        }

        if (!IsAllowedBrandingContentType(file.ContentType, extension))
        {
            return DataResponse<BrandingAssetUploadResultDto>.Fail("Unsupported file content type.", 400);
        }

        var settingName = settingKey;
        var setting = await SpecificationEvaluator<Setting>.GetQuery(
                _settingRepository.Query(asNoTracking: false),
                new QuerySpecification<Setting>(s => s.Name == settingName))
            .FirstOrDefaultAsync();

        var previousPath = setting?.Value;
        var uploadFolder = BrandingFolderName;

        try
        {
            await using var stream = file.OpenReadStream();
            var uploadedPath = await _storageService.UploadAsync(stream, file.FileName, uploadFolder);

            if (setting == null)
            {
                setting = new Setting
                {
                    Name = settingName,
                    Value = uploadedPath,
                };
                await _settingRepository.AddAsync(setting);
            }
            else
            {
                setting.Value = uploadedPath;
            }

            await _settingRepository.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(previousPath) &&
                !string.Equals(previousPath, uploadedPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _storageService.DeleteAsync(previousPath);
                }
                catch
                {
                    // Best effort cleanup; do not fail the request.
                }
            }

            return DataResponse<BrandingAssetUploadResultDto>.Succeed(
                new BrandingAssetUploadResultDto
                {
                    Url = _storageService.GetFileUrl(uploadedPath),
                },
                _localizer[LocalizationKeys.SystemSettingsUpdatedSuccessfully]);
        }
        catch (ArgumentException ex)
        {
            return DataResponse<BrandingAssetUploadResultDto>.Fail(ex.Message, 400);
        }
        catch
        {
            return DataResponse<BrandingAssetUploadResultDto>.Fail("Branding file upload failed.", 500);
        }
    }

    private static bool IsAllowedBrandingContentType(string? contentType, string extension)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        if (!AllowedBrandingContentTypes.TryGetValue(extension, out var allowedContentTypes))
        {
            return false;
        }

        var normalizedContentType = contentType.Split(';', 2)[0].Trim();
        return allowedContentTypes.Contains(normalizedContentType);
    }

    private async Task<string?> GetScopedSettingValueAsync(string settingName)
    {
        return await SpecificationEvaluator<Setting>.GetQuery(
                _settingRepository.Query(),
                new QuerySpecification<Setting>(s => s.Name == settingName))
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
    }

    private static bool GetBooleanSettingValue(IReadOnlyDictionary<string, string> settingsMap, string settingName)
    {
        return ParseBooleanSetting(
            settingsMap.GetValueOrDefault(settingName),
            GetDefaultBooleanValue(settingName));
    }

    private static int GetIntSettingValue(
        IReadOnlyDictionary<string, string> settingsMap,
        string settingName,
        int fallback,
        Func<int, int> normalize)
    {
        var defaultValue = GetDefaultIntValue(settingName, fallback);
        var parsed = ParseIntSetting(settingsMap.GetValueOrDefault(settingName), defaultValue);
        return normalize(parsed);
    }

    private static DateTime? GetDateTimeSettingValue(IReadOnlyDictionary<string, string> settingsMap, string settingName)
    {
        var value = settingsMap.GetValueOrDefault(settingName);
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (!DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            return null;
        }

        return parsed.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : parsed;
    }

    private static string GetStringSettingValue(
        IReadOnlyDictionary<string, string> settingsMap,
        string settingName,
        string fallback)
    {
        var value = settingsMap.GetValueOrDefault(settingName);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool GetDefaultBooleanValue(string settingName)
    {
        if (AuthSettingDefaults.TryGetValue(settingName, out var defaultValue))
        {
            return ParseBooleanSetting(defaultValue, true);
        }

        return true;
    }

    private static int GetDefaultIntValue(string settingName, int fallback)
    {
        if (AuthSettingDefaults.TryGetValue(settingName, out var defaultValue))
        {
            return ParseIntSetting(defaultValue, fallback);
        }

        return fallback;
    }

    private static bool ParseBooleanSetting(string? value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        if (string.Equals(value, "1", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(value, "0", StringComparison.Ordinal))
        {
            return false;
        }

        return fallback;
    }

    private static int ParseIntSetting(string? value, int fallback)
    {
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static int NormalizeEmailVerificationCodeExpirySeconds(int value)
    {
        return Math.Clamp(
            value,
            MinEmailVerificationCodeExpirySeconds,
            MaxEmailVerificationCodeExpirySeconds);
    }

    private static int NormalizeEmailVerificationResendCooldownSeconds(int value)
    {
        return Math.Clamp(
            value,
            MinEmailVerificationResendCooldownSeconds,
            MaxEmailVerificationResendCooldownSeconds);
    }

    private static int NormalizeSessionAccessTokenLifetimeMinutes(int value)
    {
        return Math.Clamp(
            value,
            MinSessionAccessTokenLifetimeMinutes,
            MaxSessionAccessTokenLifetimeMinutes);
    }

    private static int NormalizeSessionRefreshTokenLifetimeDays(int value)
    {
        return Math.Clamp(
            value,
            MinSessionRefreshTokenLifetimeDays,
            MaxSessionRefreshTokenLifetimeDays);
    }

    private static int NormalizeSessionMaxActiveSessions(int value)
    {
        return Math.Clamp(
            value,
            MinSessionMaxActiveSessions,
            MaxSessionMaxActiveSessions);
    }

    private static int NormalizeSessionIdleTimeoutMinutes(int value)
    {
        return Math.Clamp(
            value,
            MinSessionIdleTimeoutMinutes,
            MaxSessionIdleTimeoutMinutes);
    }

    private static int NormalizeSessionWarningBeforeTimeoutMinutes(int value)
    {
        return Math.Clamp(
            value,
            MinSessionWarningBeforeTimeoutMinutes,
            MaxSessionWarningBeforeTimeoutMinutes);
    }

    private static int NormalizeSessionRememberMeDurationDays(int value)
    {
        return Math.Clamp(
            value,
            MinSessionRememberMeDurationDays,
            MaxSessionRememberMeDurationDays);
    }

    private static int NormalizePasswordMinLength(int value)
    {
        return Math.Clamp(
            value,
            MinPasswordMinLength,
            MaxPasswordMinLength);
    }

    private static int NormalizeMfaTrustedDeviceDurationDays(int value)
    {
        return Math.Clamp(
            value,
            MinMfaTrustedDeviceDurationDays,
            MaxMfaTrustedDeviceDurationDays);
    }

    private static int NormalizeAccountInactiveLockDays(int value)
    {
        return Math.Clamp(
            value,
            MinAccountInactiveLockDays,
            MaxAccountInactiveLockDays);
    }

    private static int NormalizeAccountPasswordExpiryDays(int value)
    {
        return Math.Clamp(
            value,
            MinAccountPasswordExpiryDays,
            MaxAccountPasswordExpiryDays);
    }

    private static int NormalizeLoginMaxFailedAttempts(int value)
    {
        return Math.Clamp(
            value,
            MinLoginMaxFailedAttempts,
            MaxLoginMaxFailedAttempts);
    }

    private static int NormalizeLoginLockoutDurationMinutes(int value)
    {
        return Math.Clamp(
            value,
            MinLoginLockoutDurationMinutes,
            MaxLoginLockoutDurationMinutes);
    }

    private static int NormalizeEmailDailySendLimit(int value)
    {
        return Math.Clamp(
            value,
            MinEmailDailySendLimit,
            MaxEmailDailySendLimit);
    }

    private static int NormalizeEmailRetryMaxAttempts(int value)
    {
        return Math.Clamp(
            value,
            MinEmailRetryMaxAttempts,
            MaxEmailRetryMaxAttempts);
    }

    private static int NormalizeAuditRetentionDays(int value)
    {
        return Math.Clamp(
            value,
            MinAuditRetentionDays,
            MaxAuditRetentionDays);
    }

    private static int NormalizeAuditCleanupScheduleHourUtc(int value)
    {
        return Math.Clamp(
            value,
            MinAuditCleanupScheduleHourUtc,
            MaxAuditCleanupScheduleHourUtc);
    }

    private static string NormalizeTextSetting(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        trimmed = UnsafeControlCharsRegex.Replace(trimmed, " ");

        if (maxLength <= 0 || trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[..maxLength];
    }

    private static string NormalizeCultureCode(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return DefaultLocalizationCulture;
        }

        var normalized = trimmed.Replace('_', '-');
        try
        {
            return CultureInfo.GetCultureInfo(normalized).Name;
        }
        catch (CultureNotFoundException)
        {
            return DefaultLocalizationCulture;
        }
    }

    private static string NormalizeApplicationName(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return DefaultApplicationName;
        }

        return trimmed.Length > MaxApplicationNameLength
            ? trimmed[..MaxApplicationNameLength]
            : trimmed;
    }

    private string? BuildPublicAssetUrl(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        return _storageService.GetFileUrl(storedPath);
    }

    private static string NormalizeEmailAddress(string? value)
    {
        var normalized = NormalizeTextSetting(value, 200);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "no-reply@eskineria.local";
        }

        try
        {
            var parsed = new MailAddress(normalized);
            return parsed.Address;
        }
        catch (FormatException)
        {
            return "no-reply@eskineria.local";
        }
    }
}
