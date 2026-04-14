using Eskineria.Core.Settings.Entities;
using Eskineria.Core.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Eskineria.Core.Settings.Services;

public sealed class SystemSettingsSeedService
{
    private readonly DbContext _dbContext;

    public SystemSettingsSeedService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedStartupAsync(IConfiguration configuration)
    {
        var settingsToSeed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemSettingKeys.AuthLoginEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:LoginEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthRegisterEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:RegisterEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthGoogleLoginEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:GoogleLoginEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthForgotPasswordEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:ForgotPasswordEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthChangePasswordEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:ChangePasswordEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthSessionManagementEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:SessionManagementEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthEmailVerificationRequired] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:EmailVerificationRequired") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthEmailVerificationCodeExpirySeconds] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:EmailVerificationCodeExpirySeconds") ?? 180).ToString(),
            [SystemSettingKeys.AuthEmailVerificationResendCooldownSeconds] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:EmailVerificationResendCooldownSeconds") ?? 60).ToString(),
            [SystemSettingKeys.AuthSessionAccessTokenLifetimeMinutes] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:SessionAccessTokenLifetimeMinutes") ?? 60).ToString(),
            [SystemSettingKeys.AuthSessionRefreshTokenLifetimeDays] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:SessionRefreshTokenLifetimeDays") ?? 7).ToString(),
            [SystemSettingKeys.AuthSessionMaxActiveSessions] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:SessionMaxActiveSessions") ?? 5).ToString(),
            [SystemSettingKeys.AuthSessionIdleTimeoutMinutes] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:SessionIdleTimeoutMinutes") ?? 60).ToString(),
            [SystemSettingKeys.AuthSessionWarningBeforeTimeoutMinutes] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:SessionWarningBeforeTimeoutMinutes") ?? 2).ToString(),
            [SystemSettingKeys.AuthSessionRememberMeDurationDays] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:SessionRememberMeDurationDays") ?? 30).ToString(),
            [SystemSettingKeys.AuthSessionSingleDeviceModeEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:SessionSingleDeviceModeEnabled") ?? false).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthPasswordPolicyMinLength] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:PasswordMinLength") ?? 8).ToString(),
            [SystemSettingKeys.AuthPasswordPolicyRequireUppercase] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:PasswordRequireUppercase") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthPasswordPolicyRequireLowercase] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:PasswordRequireLowercase") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthPasswordPolicyRequireDigit] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:PasswordRequireDigit") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthPasswordPolicyRequireNonAlphanumeric] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:PasswordRequireNonAlphanumeric") ?? false).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthMfaEnforceForAdmins] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:MfaEnforceForAdmins") ?? false).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthMfaTrustedDeviceDurationDays] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:MfaTrustedDeviceDurationDays") ?? 30).ToString(),
            [SystemSettingKeys.AuthMfaBypassIpWhitelist] = configuration.GetValue<string?>("StartupSeed:SystemSettings:Auth:MfaBypassIpWhitelist") ?? string.Empty,
            [SystemSettingKeys.AuthRegistrationInvitationRequired] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:RegistrationInvitationRequired") ?? false).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthRegistrationAllowedEmailDomains] = configuration.GetValue<string?>("StartupSeed:SystemSettings:Auth:RegistrationAllowedEmailDomains") ?? string.Empty,
            [SystemSettingKeys.AuthRegistrationBlockedEmailDomains] = configuration.GetValue<string?>("StartupSeed:SystemSettings:Auth:RegistrationBlockedEmailDomains") ?? string.Empty,
            [SystemSettingKeys.AuthRegistrationAutoApproveEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:RegistrationAutoApproveEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthAccountLifecycleInactiveLockDays] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:AccountInactiveLockDays") ?? 90).ToString(),
            [SystemSettingKeys.AuthAccountLifecycleForcePasswordChangeOnFirstLogin] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:AccountForcePasswordChangeOnFirstLogin") ?? false).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthAccountLifecyclePasswordExpiryDays] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:AccountPasswordExpiryDays") ?? 90).ToString(),
            [SystemSettingKeys.AuthLoginSecurityLockoutEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:Auth:LoginLockoutEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.AuthLoginSecurityMaxFailedAttempts] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:LoginMaxFailedAttempts") ?? 5).ToString(),
            [SystemSettingKeys.AuthLoginSecurityLockoutDurationMinutes] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:Auth:LoginLockoutDurationMinutes") ?? 15).ToString(),
            [SystemSettingKeys.SystemMaintenanceModeEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:System:MaintenanceModeEnabled") ?? false).ToString().ToLowerInvariant(),
            [SystemSettingKeys.SystemMaintenanceEndTime] = configuration.GetValue<string?>("StartupSeed:SystemSettings:System:MaintenanceEndTime") ?? string.Empty,
            [SystemSettingKeys.SystemMaintenanceMessage] = configuration.GetValue<string?>("StartupSeed:SystemSettings:System:MaintenanceMessage") ?? string.Empty,
            [SystemSettingKeys.SystemMaintenanceCountdownEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:System:MaintenanceCountdownEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.SystemMaintenanceIpWhitelist] = configuration.GetValue<string?>("StartupSeed:SystemSettings:System:MaintenanceIpWhitelist") ?? string.Empty,
            [SystemSettingKeys.SystemMaintenanceRoleWhitelist] = configuration.GetValue<string?>("StartupSeed:SystemSettings:System:MaintenanceRoleWhitelist") ?? "Admin",
            [SystemSettingKeys.SystemBrandingApplicationName] = configuration.GetValue<string?>("StartupSeed:SystemSettings:Branding:ApplicationName") ?? "Eskineria Backend",
            [SystemSettingKeys.SystemEmailSenderName] = configuration.GetValue<string?>("StartupSeed:SystemSettings:System:EmailSenderName") ?? "Eskineria",
            [SystemSettingKeys.SystemEmailSenderAddress] = configuration.GetValue<string?>("StartupSeed:SystemSettings:System:EmailSenderAddress") ?? "no-reply@eskineria.local",
            [SystemSettingKeys.SystemEmailDailySendLimit] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:System:EmailDailySendLimit") ?? 5000).ToString(),
            [SystemSettingKeys.SystemEmailRetryMaxAttempts] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:System:EmailRetryMaxAttempts") ?? 3).ToString(),
            [SystemSettingKeys.SystemNotificationsLoginAlertEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:System:NotificationLoginAlertEnabled") ?? true).ToString().ToLowerInvariant(),
            [SystemSettingKeys.SystemNotificationsSecurityEmailRecipients] = configuration.GetValue<string?>("StartupSeed:SystemSettings:System:NotificationSecurityEmailRecipients") ?? string.Empty,
            [SystemSettingKeys.SystemLocalizationDefaultCulture] = configuration.GetValue<string?>("StartupSeed:SystemSettings:System:LocalizationDefaultCulture") ?? "en-US",
            [SystemSettingKeys.SystemLocalizationFallbackCulture] = configuration.GetValue<string?>("StartupSeed:SystemSettings:System:LocalizationFallbackCulture") ?? "en-US",
            [SystemSettingKeys.SystemLocalizationRequireUserCultureSelection] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:System:LocalizationRequireUserCultureSelection") ?? false).ToString().ToLowerInvariant(),
            [SystemSettingKeys.SystemAuditRetentionDays] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:System:AuditRetentionDays") ?? 365).ToString(),
            [SystemSettingKeys.SystemAuditCleanupScheduleHourUtc] = (configuration.GetValue<int?>("StartupSeed:SystemSettings:System:AuditCleanupScheduleHourUtc") ?? 2).ToString(),
            [SystemSettingKeys.SystemAuditPiiMaskingEnabled] = (configuration.GetValue<bool?>("StartupSeed:SystemSettings:System:AuditPiiMaskingEnabled") ?? true).ToString().ToLowerInvariant()
        };

        var existingSettings = await _dbContext.Set<Setting>()
            .Where(x => settingsToSeed.Keys.Contains(x.Name))
            .ToListAsync();

        var existingSettingNames = existingSettings
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingSettings = settingsToSeed
            .Where(x => !existingSettingNames.Contains(x.Key))
            .Select(x => new Setting
            {
                Name = x.Key,
                Value = x.Value
            })
            .ToList();

        if (missingSettings.Count == 0)
        {
            return;
        }

        await _dbContext.Set<Setting>().AddRangeAsync(missingSettings);
        await _dbContext.SaveChangesAsync();
        Console.WriteLine($"[StartupSeed] Seeded {missingSettings.Count} system setting(s).");
    }
}
