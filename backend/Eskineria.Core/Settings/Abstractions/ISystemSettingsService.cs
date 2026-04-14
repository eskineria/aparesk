using Eskineria.Core.Settings.Models;
using Eskineria.Core.Shared.Response;
using Microsoft.AspNetCore.Http;

namespace Eskineria.Core.Settings.Abstractions;

public interface ISystemSettingsService
{
    Task<DataResponse<AuthSystemSettingsDto>> GetAuthSettingsAsync();
    Task<Response> UpdateAuthSettingsAsync(UpdateAuthSystemSettingsRequest request);
    Task<DataResponse<BrandingAssetUploadResultDto>> UploadApplicationLogoAsync(IFormFile file);
    Task<DataResponse<BrandingAssetUploadResultDto>> UploadApplicationFaviconAsync(IFormFile file);
    Task<bool> IsLoginEnabledAsync();
    Task<bool> IsRegistrationEnabledAsync();
    Task<bool> IsGoogleLoginEnabledAsync();
    Task<bool> IsForgotPasswordEnabledAsync();
    Task<bool> IsChangePasswordEnabledAsync();
    Task<bool> IsSessionManagementEnabledAsync();
    Task<bool> IsEmailVerificationRequiredAsync();
    Task<int> GetEmailVerificationCodeExpirySecondsAsync();
    Task<int> GetEmailVerificationResendCooldownSecondsAsync();
    Task<int> GetSessionAccessTokenLifetimeMinutesAsync();
    Task<int> GetSessionRefreshTokenLifetimeDaysAsync();
    Task<int> GetSessionMaxActiveSessionsAsync();
    Task<int> GetSessionIdleTimeoutMinutesAsync();
    Task<int> GetSessionWarningBeforeTimeoutMinutesAsync();
    Task<int> GetSessionRememberMeDurationDaysAsync();
    Task<bool> IsSessionSingleDeviceModeEnabledAsync();
    Task<int> GetPasswordMinLengthAsync();
    Task<bool> IsPasswordRequireUppercaseAsync();
    Task<bool> IsPasswordRequireLowercaseAsync();
    Task<bool> IsPasswordRequireDigitAsync();
    Task<bool> IsPasswordRequireNonAlphanumericAsync();
    Task<bool> IsLoginLockoutEnabledAsync();
    Task<int> GetLoginMaxFailedAttemptsAsync();
    Task<int> GetLoginLockoutDurationMinutesAsync();
    Task<bool> IsNotificationLoginAlertEnabledAsync();
    Task<bool> IsMfaEnabledAsync();
    Task<bool> IsMaintenanceModeEnabledAsync();
}
