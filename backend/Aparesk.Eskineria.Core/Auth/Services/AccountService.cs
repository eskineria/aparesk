using System.Security.Claims;
using Aparesk.Eskineria.Core.Settings.Abstractions;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Auth.Entities;
using Aparesk.Eskineria.Core.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Aparesk.Eskineria.Core.Notifications.Abstractions;
using Aparesk.Eskineria.Core.Notifications.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Aparesk.Eskineria.Core.Auth.Utilities;

namespace Aparesk.Eskineria.Core.Auth.Services;

public class AccountService : IAccountService
{
    private const int PasswordResetMaxFailedAttempts = 5;
    private const int MinEmailVerificationCodeExpirySeconds = 30;
    private const int MaxEmailVerificationCodeExpirySeconds = 1800;
    private const int MinEmailVerificationResendCooldownSeconds = 5;
    private const int MaxEmailVerificationResendCooldownSeconds = 600;
    private const int MinPasswordMinLength = 6;
    private const int MaxPasswordMinLength = 128;

    private readonly UserManager<EskineriaUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly INotificationService _notificationService;
    private readonly AuthEmailTemplateHelper _emailTemplateHelper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IStringLocalizer<AccountService> _localizer;

    public AccountService(
        UserManager<EskineriaUser> userManager,
        ITokenService tokenService,
        INotificationService notificationService,
        AuthEmailTemplateHelper emailTemplateHelper,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ISystemSettingsService systemSettingsService,
        IStringLocalizer<AccountService> localizer)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _notificationService = notificationService;
        _emailTemplateHelper = emailTemplateHelper;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _systemSettingsService = systemSettingsService;
        _localizer = localizer;
    }

    public async Task<AuthResponse> ChangePasswordAsync(
        ClaimsPrincipal userPrincipal,
        ChangePasswordRequest request,
        AuthRuntimeSettings? runtimeSettings = null)
    {
        var normalizedSettings = NormalizeRuntimeSettings(runtimeSettings ?? new AuthRuntimeSettings());
        ApplyPasswordPolicyToIdentityOptions(normalizedSettings);

        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null)
            return new AuthResponse { Success = false, Message = _localizer["UserNotFound"] };

        IdentityResult result;
        if (!await _userManager.HasPasswordAsync(user))
        {
            result = await _userManager.AddPasswordAsync(user, request.NewPassword);
        }
        else
        {
            result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }

        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["PasswordChangeFailed"],
                Errors = result.Errors.Select(e => e.Description)
            };
        }

        await _tokenService.RevokeUserRefreshTokensAsync(user.Id);
        await TrySendPasswordChangedNotificationAsync(user, GetCurrentIpAddress(), GetCurrentUserAgent());
        return new AuthResponse { Success = true, Message = _localizer["PasswordChangedSuccessfully"] };
    }

    public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request, AuthRuntimeSettings? runtimeSettings = null)
    {
        var normalizedSettings = NormalizeRuntimeSettings(runtimeSettings ?? new AuthRuntimeSettings());
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return new AuthResponse { Success = true, Message = _localizer["PasswordResetLinkSent"] };
        }

        var emailResult = await SendPasswordResetCodeAsync(user, normalizedSettings);
        if (!emailResult.Success)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["SmtpDeliveryFailedError"] ?? "E-posta gönderimi başarısız oldu."
            };
        }

        return new AuthResponse
        {
            Success = true,
            Message = _localizer["PasswordResetLinkSent"]
        };
    }

    public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request, AuthRuntimeSettings? runtimeSettings = null)
    {
        var normalizedSettings = NormalizeRuntimeSettings(runtimeSettings ?? new AuthRuntimeSettings());
        ApplyPasswordPolicyToIdentityOptions(normalizedSettings);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return new AuthResponse { Success = true, Message = _localizer["PasswordResetSuccessful"] };

        var codeValidationResult = await ValidatePasswordResetCodeAsync(user, request.Code);
        if (!codeValidationResult.Success)
        {
            return codeValidationResult;
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["PasswordResetFailed"],
                Errors = result.Errors.Select(e => e.Description)
            };
        }

        ClearPasswordResetState(user);
        await _userManager.UpdateAsync(user);
        await _tokenService.RevokeUserRefreshTokensAsync(user.Id);
        await TrySendPasswordChangedNotificationAsync(user, GetCurrentIpAddress(), GetCurrentUserAgent());
        
        // Also send welcome email if not sent yet and user is now fully verified
        if (user.EmailConfirmed)
        {
            await TrySendWelcomeEmailAsync(user);
        }

        return new AuthResponse { Success = true, Message = _localizer["PasswordResetSuccessful"] };
    }

    public async Task<AuthResponse> VerifyPasswordResetCodeAsync(VerifyPasswordResetCodeRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = _localizer["InvalidRequest"] };
        }

        return await ValidatePasswordResetCodeAsync(user, request.Code, incrementFailedAttempts: true, clearOnSuccess: false);
    }

    public async Task<AuthResponse> ResendPasswordResetCodeAsync(ResendPasswordResetCodeRequest request, AuthRuntimeSettings? runtimeSettings = null)
    {
        var normalizedSettings = NormalizeRuntimeSettings(runtimeSettings ?? new AuthRuntimeSettings());
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return new AuthResponse { Success = true, Message = _localizer["PasswordResetCodeSentGeneric"] };
        }

        if (user.PasswordResetCodeSentAtUtc.HasValue)
        {
            var resendAt = user.PasswordResetCodeSentAtUtc.Value.AddSeconds(normalizedSettings.EmailVerificationResendCooldownSeconds);
            if (resendAt > DateTime.UtcNow)
            {
                var secondsLeft = (int)Math.Ceiling((resendAt - DateTime.UtcNow).TotalSeconds);
                return new AuthResponse
                {
                    Success = false,
                    Message = _localizer["PasswordResetResendTooSoon", secondsLeft]
                };
            }
        }

        var emailResult = await SendPasswordResetCodeAsync(user, normalizedSettings, isResend: true);
        if (!emailResult.Success)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["SmtpDeliveryFailedError"] ?? "E-posta gönderimi başarısız oldu."
            };
        }

        return new AuthResponse { Success = true, Message = _localizer["PasswordResetCodeResent"] };
    }

    public async Task<AuthResponse> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return new AuthResponse { Success = false, Message = _localizer["InvalidRequest"] };

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return new AuthResponse { Success = false, Message = _localizer["EmailAlreadyConfirmed"] };
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);

        if (!result.Succeeded)
        {
             return new AuthResponse
             {
                 Success = false,
                 Message = _localizer["EmailConfirmationFailed"],
                 Errors = result.Errors.Select(e => e.Description)
             };
        }

        // Force security stamp update to invalidate the token immediately
        await _userManager.UpdateSecurityStampAsync(user);

        return new AuthResponse { Success = true, Message = _localizer["EmailConfirmedSuccessfully"] };
    }

    public async Task<AuthResponse<UserInfoDto>> GetUserInfoAsync(ClaimsPrincipal userPrincipal)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null)
            return new AuthResponse<UserInfoDto> { Success = false, Message = _localizer["UserNotFound"] };

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse<UserInfoDto>
        {
            Success = true,
            Data = new UserInfoDto
            {
                Id = user.Id.ToString(),
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                ProfilePicture = user.ProfilePicture,
                Roles = roles.OrderBy(r => r).ToList(),
                ActiveRole = user.ActiveRole,
                HasPassword = !string.IsNullOrEmpty(user.PasswordHash)
            }
        };
    }

    public async Task<AuthResponse<MfaStatusDto>> GetMfaStatusAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            return new AuthResponse<MfaStatusDto> { Success = false, Message = _localizer["UserNotFound"] };
        }

        return new AuthResponse<MfaStatusDto>
        {
            Success = true,
            Data = new MfaStatusDto
            {
                Enabled = user.TwoFactorEnabled,
                FeatureEnabled = await _systemSettingsService.IsMfaEnabledAsync()
            }
        };
    }

    public async Task<AuthResponse<List<UserSessionDto>>> GetUserSessionsAsync(ClaimsPrincipal userPrincipal, string? currentRefreshToken)
    {
        var userId = GetCurrentUserId(userPrincipal);
        if (!userId.HasValue)
        {
            return new AuthResponse<List<UserSessionDto>> { Success = false, Message = _localizer["UserNotFound"] };
        }

        Guid? currentSessionId = null;
        if (!string.IsNullOrWhiteSpace(currentRefreshToken))
        {
            currentSessionId = await _tokenService.GetSessionIdAsync(currentRefreshToken);
        }

        var sessions = await _tokenService.GetUserSessionsAsync(userId.Value, currentSessionId);
        return new AuthResponse<List<UserSessionDto>>
        {
            Success = true,
            Message = _localizer["SessionsRetrievedSuccessfully"],
            Data = sessions.ToList()
        };
    }

    public async Task<AuthResponse> RevokeSessionAsync(ClaimsPrincipal userPrincipal, Guid sessionId)
    {
        var userId = GetCurrentUserId(userPrincipal);
        if (!userId.HasValue)
        {
            return new AuthResponse { Success = false, Message = _localizer["UserNotFound"] };
        }

        var isRevoked = await _tokenService.RevokeSessionAsync(userId.Value, sessionId);
        if (!isRevoked)
        {
            return new AuthResponse { Success = false, Message = _localizer["SessionNotFound"] };
        }

        return new AuthResponse { Success = true, Message = _localizer["SessionRevoked"] };
    }

    public async Task<AuthResponse> RevokeOtherSessionsAsync(ClaimsPrincipal userPrincipal, string? currentRefreshToken)
    {
        var userId = GetCurrentUserId(userPrincipal);
        if (!userId.HasValue)
        {
            return new AuthResponse { Success = false, Message = _localizer["UserNotFound"] };
        }

        if (string.IsNullOrWhiteSpace(currentRefreshToken))
        {
            return new AuthResponse { Success = false, Message = _localizer["SessionNotFound"] };
        }

        var currentSessionId = await _tokenService.GetSessionIdAsync(currentRefreshToken);
        if (!currentSessionId.HasValue)
        {
            return new AuthResponse { Success = false, Message = _localizer["SessionNotFound"] };
        }

        var sessions = await _tokenService.GetUserSessionsAsync(userId.Value, currentSessionId);
        if (!sessions.Any(x => x.Id == currentSessionId.Value))
        {
            return new AuthResponse { Success = false, Message = _localizer["SessionNotFound"] };
        }

        var revokedCount = await _tokenService.RevokeOtherSessionsAsync(userId.Value, currentSessionId.Value);
        return new AuthResponse { Success = true, Message = _localizer["OtherSessionsRevoked", revokedCount] };
    }

    public async Task<AuthResponse> UpdateUserInfoAsync(ClaimsPrincipal userPrincipal, UpdateUserInfoRequest request)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null)
            return new AuthResponse { Success = false, Message = _localizer["UserNotFound"] };

        var requestedNormalizedEmail = _userManager.NormalizeEmail(request.Email.Trim()) ?? string.Empty;
        var currentNormalizedEmail = _userManager.NormalizeEmail(user.Email ?? string.Empty) ?? string.Empty;
        if (!string.Equals(requestedNormalizedEmail, currentNormalizedEmail, StringComparison.Ordinal))
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["ProfileEmailChangeDisabled"]
            };
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.ProfilePicture = string.IsNullOrWhiteSpace(request.ProfilePicture)
            ? null
            : request.ProfilePicture.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["UpdateFailed"],
                Errors = result.Errors.Select(e => e.Description)
            };
        }

        return new AuthResponse { Success = true, Message = _localizer["UpdateSuccessful"] };
    }

    public async Task SendLoginNotificationAsync(Guid userId, string? ipAddress, string? userAgent)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        await TrySendLoginNotificationAsync(user, ipAddress, userAgent);
    }

    public async Task<AuthResponse> SendMfaActionCodeAsync(ClaimsPrincipal userPrincipal, bool targetState)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return new AuthResponse { Success = false, Message = _localizer["UserNotFound"] };
        }

        if (user.TwoFactorEnabled == targetState)
        {
            return new AuthResponse
            {
                Success = true,
                Message = _localizer[targetState ? "MfaAlreadyEnabled" : "MfaAlreadyDisabled"]
            };
        }

        var verificationCode = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

        var templateModel = new
        {
            MfaActionEmailTitle = _localizer[targetState ? "MfaEnableEmailTitle" : "MfaDisableEmailTitle"].Value,
            Greeting = _localizer["EmailGreeting", user.FirstName ?? string.Empty].Value,
            MfaActionEmailContent = _localizer[targetState ? "MfaEnableEmailContent" : "MfaDisableEmailContent"].Value,
            VerificationCodeLabel = _localizer["VerificationCodeLabel"].Value,
            VerificationCode = verificationCode,
            EmailSecurityNote = _localizer["EmailSecurityNote"].Value,
            EmailTeam = _localizer["EmailTeam"].Value,
            EmailFooterIgnore = _localizer["EmailFooterIgnore"].Value
        };

        var fallbackSubject = _localizer["MfaActionEmailSubject"].Value;
        var fallbackBody = _localizer["MfaActionEmailFallback", verificationCode].Value;

        var loadedTemplate = await _emailTemplateHelper.LoadTemplateAsync("MfaActionCode", "MfaActionCode", user.Email);
        var emailSubject = await _emailTemplateHelper.RenderSubjectAsync(loadedTemplate.Subject, templateModel, fallbackSubject);
        var emailBody = await _emailTemplateHelper.RenderBodyAsync(loadedTemplate.Body, templateModel, fallbackBody);

        var result = await _notificationService.SendAsync(new NotificationMessage
        {
            Recipient = user.Email,
            Title = emailSubject,
            Body = emailBody,
            Channel = NotificationChannel.Email,
            Data = new Dictionary<string, object>
            {
                ["TemplateKey"] = loadedTemplate.TrackingKey,
                ["Culture"] = loadedTemplate.Culture,
                ["CorrelationId"] = Guid.NewGuid().ToString("N"),
                ["RequestedByUserId"] = user.Id.ToString(),
            }
        });

        return result.Success
            ? new AuthResponse { Success = true, Message = _localizer["MfaCodeSent"] }
            : new AuthResponse { Success = false, Message = _localizer["MfaCodeDeliveryFailed"] };
    }

    private Guid? GetCurrentUserId(ClaimsPrincipal userPrincipal)
    {
        var userId = _userManager.GetUserId(userPrincipal);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task<NotificationResult> SendPasswordResetCodeAsync(
        EskineriaUser user,
        AuthRuntimeSettings runtimeSettings,
        bool isResend = false)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("User email is required for password reset.");
        }

        var verificationCode = VerificationCodeHelper.GenerateCode();
        var now = DateTime.UtcNow;

        user.PasswordResetCodeHash = VerificationCodeHelper.HashCode(verificationCode, user.SecurityStamp);
        user.PasswordResetCodeExpiresAtUtc = now.AddSeconds(runtimeSettings.EmailVerificationCodeExpirySeconds);
        user.PasswordResetCodeSentAtUtc = now;
        user.PasswordResetFailedAttempts = 0;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException("Could not persist password reset code for the user.");
        }

        var occurredAtUtc = DateTime.UtcNow;
        var eventDateTime = FormatEventDateTime(occurredAtUtc);
        var resolvedLocation = ResolveLocation(GetCurrentIpAddress());
        var resolvedDevice = ResolveDevice(GetCurrentUserAgent());

        var templateModel = new
        {
            ResetPasswordEmailTitle = _localizer["ResetPasswordEmailTitle"].Value,
            Greeting = _localizer["EmailGreeting", user.FirstName ?? string.Empty].Value,
            ResetPasswordEmailContent = _localizer["PasswordResetCodeEmailContent"].Value,
            VerificationCodeLabel = _localizer["VerificationCodeLabel"].Value,
            VerificationCode = verificationCode,
            ResetPasswordLink = GetResetPasswordUrl(user.Email, verificationCode),
            ResetPasswordButton = _localizer["ResetPasswordButton"].Value,
            SecurityAlertLabel = _localizer["SecurityAlertLabel"].Value,
            SecurityEventTimeLabel = _localizer["SecurityEventTimeLabel"].Value,
            SecurityEventLocationLabel = _localizer["SecurityEventLocationLabel"].Value,
            SecurityEventDeviceLabel = _localizer["SecurityEventDeviceLabel"].Value,
            EventDateTime = eventDateTime,
            LoginLocation = resolvedLocation,
            DeviceInfo = resolvedDevice,
            EmailSecurityNote = _localizer["EmailSecurityNote"].Value,
            ResetPasswordEmailExpiry = _localizer["PasswordResetCodeEmailExpiry", (int)runtimeSettings.EmailVerificationCodeExpirySeconds].Value,
            EmailTeam = _localizer["EmailTeam"].Value,
            EmailFooterIgnore = _localizer["EmailFooterIgnore"].Value,
            IsResend = isResend
        };

        var fallbackSubject = _localizer["PasswordResetEmailSubject"].Value;
        var fallbackBody = _localizer["PasswordResetCodeFallback", verificationCode, runtimeSettings.EmailVerificationCodeExpirySeconds].Value;
        var loadedTemplate = await _emailTemplateHelper.LoadTemplateAsync("ResetPassword", "ResetPassword", user.Email);
        var emailSubject = await _emailTemplateHelper.RenderSubjectAsync(loadedTemplate.Subject, templateModel, fallbackSubject);
        var emailBody = await _emailTemplateHelper.RenderBodyAsync(loadedTemplate.Body, templateModel, fallbackBody);

        return await _notificationService.SendAsync(new NotificationMessage
        {
            Recipient = user.Email,
            Title = emailSubject,
            Body = emailBody,
            Channel = NotificationChannel.Email,
            Data = new Dictionary<string, object>
            {
                ["TemplateKey"] = loadedTemplate.TrackingKey,
                ["Culture"] = loadedTemplate.Culture,
                ["CorrelationId"] = Guid.NewGuid().ToString("N"),
                ["RequestedByUserId"] = user.Id.ToString(),
            }
        });
    }

    private async Task<AuthResponse> ValidatePasswordResetCodeAsync(
        EskineriaUser user,
        string code,
        bool incrementFailedAttempts = true,
        bool clearOnSuccess = false)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordResetCodeHash) ||
            !user.PasswordResetCodeExpiresAtUtc.HasValue)
        {
            return new AuthResponse { Success = false, Message = _localizer["PasswordResetCodeExpired"] };
        }

        if (user.PasswordResetCodeExpiresAtUtc.Value < DateTime.UtcNow)
        {
            ClearPasswordResetState(user);
            await _userManager.UpdateAsync(user);
            return new AuthResponse { Success = false, Message = _localizer["PasswordResetCodeExpired"] };
        }

        if (user.PasswordResetFailedAttempts >= PasswordResetMaxFailedAttempts)
        {
            ClearPasswordResetState(user);
            await _userManager.UpdateAsync(user);
            return new AuthResponse { Success = false, Message = _localizer["PasswordResetTooManyAttempts"] };
        }

        if (!VerificationCodeHelper.IsCodeValid(user.PasswordResetCodeHash, user.SecurityStamp, code))
        {
            if (incrementFailedAttempts)
            {
                user.PasswordResetFailedAttempts += 1;

                if (user.PasswordResetFailedAttempts >= PasswordResetMaxFailedAttempts)
                {
                    ClearPasswordResetState(user);
                    await _userManager.UpdateAsync(user);
                    return new AuthResponse { Success = false, Message = _localizer["PasswordResetTooManyAttempts"] };
                }

                await _userManager.UpdateAsync(user);
            }

            return new AuthResponse { Success = false, Message = _localizer["PasswordResetCodeInvalid"] };
        }

        if (clearOnSuccess)
        {
            ClearPasswordResetState(user);
            await _userManager.UpdateAsync(user);
        }

        return new AuthResponse { Success = true };
    }

    private static void ClearPasswordResetState(EskineriaUser user)
    {
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiresAtUtc = null;
        user.PasswordResetCodeSentAtUtc = null;
        user.PasswordResetFailedAttempts = 0;
    }

    private static AuthRuntimeSettings NormalizeRuntimeSettings(AuthRuntimeSettings runtimeSettings)
    {
        var settings = runtimeSettings ?? new AuthRuntimeSettings();
        settings.EmailVerificationCodeExpirySeconds = Math.Clamp(
            settings.EmailVerificationCodeExpirySeconds,
            MinEmailVerificationCodeExpirySeconds,
            MaxEmailVerificationCodeExpirySeconds);
        settings.EmailVerificationResendCooldownSeconds = Math.Clamp(
            settings.EmailVerificationResendCooldownSeconds,
            MinEmailVerificationResendCooldownSeconds,
            MaxEmailVerificationResendCooldownSeconds);
        settings.PasswordMinLength = Math.Clamp(
            settings.PasswordMinLength,
            MinPasswordMinLength,
            MaxPasswordMinLength);
        return settings;
    }

    private void ApplyPasswordPolicyToIdentityOptions(AuthRuntimeSettings settings)
    {
        _userManager.Options.Password.RequiredLength = settings.PasswordMinLength;
        _userManager.Options.Password.RequireUppercase = settings.PasswordRequireUppercase;
        _userManager.Options.Password.RequireLowercase = settings.PasswordRequireLowercase;
        _userManager.Options.Password.RequireDigit = settings.PasswordRequireDigit;
        _userManager.Options.Password.RequireNonAlphanumeric = settings.PasswordRequireNonAlphanumeric;
    }

    private async Task TrySendPasswordChangedNotificationAsync(
        EskineriaUser user,
        string? ipAddress,
        string? userAgent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            var occurredAtUtc = DateTime.UtcNow;
            var eventDateTime = FormatEventDateTime(occurredAtUtc);
            var resolvedLocation = ResolveLocation(ipAddress);
            var resolvedDevice = ResolveDevice(userAgent);
            var accountUrl = $"{_configuration["FrontendUrl"] ?? "http://localhost:5173"}/users/profile";

            var templateModel = new
            {
                PasswordChangedAlertEmailTitle = _localizer["PasswordChangedAlertEmailTitle"].Value,
                Greeting = _localizer["EmailGreeting", user.FirstName ?? string.Empty].Value,
                PasswordChangedAlertEmailContent = _localizer["PasswordChangedAlertEmailContent"].Value,
                SecurityEventTimeLabel = _localizer["SecurityEventTimeLabel"].Value,
                SecurityEventLocationLabel = _localizer["SecurityEventLocationLabel"].Value,
                SecurityEventDeviceLabel = _localizer["SecurityEventDeviceLabel"].Value,
                EventDateTime = eventDateTime,
                LoginLocation = resolvedLocation,
                DeviceInfo = resolvedDevice,
                AccountSecurityActionText = _localizer["AccountSecurityActionText"].Value,
                AccountSecurityActionLink = accountUrl,
                AccountSecurityButton = _localizer["AccountSecurityButton"].Value,
                EmailTeam = _localizer["EmailTeam"].Value,
                EmailFooterIgnore = _localizer["EmailFooterIgnore"].Value,
            };

            var fallbackSubject = _localizer["PasswordChangedAlertEmailSubject"].Value;
            var fallbackBody = _localizer["PasswordChangedAlertEmailFallback", eventDateTime, resolvedLocation, resolvedDevice].Value;
            var loadedTemplate = await _emailTemplateHelper.LoadTemplateAsync("PasswordChangedAlert", "PasswordChangedAlert", user.Email);
            var emailSubject = await _emailTemplateHelper.RenderSubjectAsync(loadedTemplate.Subject, templateModel, fallbackSubject);
            var emailBody = await _emailTemplateHelper.RenderBodyAsync(loadedTemplate.Body, templateModel, fallbackBody);

            await _notificationService.SendAsync(new NotificationMessage
            {
                Recipient = user.Email,
                Title = emailSubject,
                Body = emailBody,
                Channel = NotificationChannel.Email,
                Data = new Dictionary<string, object>
                {
                    ["TemplateKey"] = loadedTemplate.TrackingKey,
                    ["Culture"] = loadedTemplate.Culture,
                    ["CorrelationId"] = Guid.NewGuid().ToString("N"),
                    ["RequestedByUserId"] = user.Id.ToString(),
                }
            });
        }
        catch
        {
            // Security notifications should not block successful auth flows.
        }
    }

    private async Task TrySendLoginNotificationAsync(
        EskineriaUser user,
        string? ipAddress,
        string? userAgent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            var occurredAtUtc = DateTime.UtcNow;
            var eventDateTime = FormatEventDateTime(occurredAtUtc);
            var resolvedLocation = ResolveLocation(ipAddress);
            var resolvedDevice = ResolveDevice(userAgent);
            var accountUrl = $"{_configuration["FrontendUrl"] ?? "http://localhost:5173"}/users/profile";

            var templateModel = new
            {
                LoginAlertEmailTitle = _localizer["LoginAlertEmailTitle"].Value,
                Greeting = _localizer["EmailGreeting", user.FirstName ?? string.Empty].Value,
                LoginAlertEmailContent = _localizer["LoginAlertEmailContent"].Value,
                SecurityEventTimeLabel = _localizer["SecurityEventTimeLabel"].Value,
                SecurityEventLocationLabel = _localizer["SecurityEventLocationLabel"].Value,
                SecurityEventDeviceLabel = _localizer["SecurityEventDeviceLabel"].Value,
                EventDateTime = eventDateTime,
                LoginLocation = resolvedLocation,
                DeviceInfo = resolvedDevice,
                AccountSecurityActionText = _localizer["AccountSecurityActionText"].Value,
                AccountSecurityActionLink = accountUrl,
                ReviewSecurityButton = _localizer["ReviewSecurityButton"].Value,
                EmailTeam = _localizer["EmailTeam"].Value,
                EmailFooterIgnore = _localizer["EmailFooterIgnore"].Value,
            };

            var fallbackSubject = _localizer["LoginAlertEmailSubject"].Value;
            var fallbackBody = _localizer["LoginAlertEmailFallback", eventDateTime, resolvedLocation, resolvedDevice].Value;
            var loadedTemplate = await _emailTemplateHelper.LoadTemplateAsync("LoginAlert", "LoginAlert", user.Email);
            var emailSubject = await _emailTemplateHelper.RenderSubjectAsync(loadedTemplate.Subject, templateModel, fallbackSubject);
            var emailBody = await _emailTemplateHelper.RenderBodyAsync(loadedTemplate.Body, templateModel, fallbackBody);

            await _notificationService.SendAsync(new NotificationMessage
            {
                Recipient = user.Email,
                Title = emailSubject,
                Body = emailBody,
                Channel = NotificationChannel.Email,
                Data = new Dictionary<string, object>
                {
                    ["TemplateKey"] = loadedTemplate.TrackingKey,
                    ["Culture"] = loadedTemplate.Culture,
                    ["CorrelationId"] = Guid.NewGuid().ToString("N"),
                    ["RequestedByUserId"] = user.Id.ToString(),
                }
            });
        }
        catch
        {
            // Security notifications should not block successful auth flows.
        }
    }

    public async Task TrySendWelcomeEmailAsync(EskineriaUser user)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return;

            var templateModel = new
            {
                WelcomeEmailTitle = _localizer["WelcomeEmailTitle"].Value,
                Greeting = _localizer["EmailGreeting", user.FirstName ?? string.Empty].Value,
                WelcomeEmailContent = _localizer["WelcomeEmailContent"].Value,
                WelcomeEmailGetStartedText = _localizer["WelcomeEmailGetStartedText"].Value,
                DashboardLink = GetDashboardUrl(),
                GoToDashboardButton = _localizer["GoToDashboardButton"].Value,
                EmailTeam = _localizer["EmailTeam"].Value,
                EmailFooterIgnore = _localizer["EmailFooterIgnore"].Value
            };

            var fallbackSubject = _localizer["WelcomeEmailSubject"].Value;
            var fallbackBody = _localizer["WelcomeEmailFallback", user.FirstName ?? "User"].Value;

            var loadedTemplate = await _emailTemplateHelper.LoadTemplateAsync("Welcome", "Welcome", user.Email);
            var emailSubject = await _emailTemplateHelper.RenderSubjectAsync(loadedTemplate.Subject, templateModel, fallbackSubject);
            var emailBody = await _emailTemplateHelper.RenderBodyAsync(loadedTemplate.Body, templateModel, fallbackBody);

            await _notificationService.SendAsync(new NotificationMessage
            {
                Recipient = user.Email,
                Title = emailSubject,
                Body = emailBody,
                Channel = NotificationChannel.Email
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send welcome email to {user.Email}: {ex.Message}");
        }
    }

    public async Task TrySendAccountLockedNotificationAsync(EskineriaUser user, int lockoutMinutes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return;

            var templateModel = new
            {
                AccountLockedEmailTitle = _localizer["AccountLockedEmailTitle"].Value,
                Greeting = _localizer["EmailGreeting", user.FirstName ?? string.Empty].Value,
                AccountLockedEmailContent = _localizer["AccountLockedEmailContent"].Value,
                SecurityAlertLabel = _localizer["SecurityAlertLabel"].Value,
                AccountLockedReasonText = _localizer["AccountLockedReasonText", lockoutMinutes].Value,
                AccountLockedActionText = _localizer["AccountLockedActionText"].Value,
                ForgotPasswordLink = GetForgotPasswordUrl(),
                ResetPasswordButton = _localizer["ResetPasswordButton"].Value,
                EmailTeam = _localizer["EmailTeam"].Value,
                EmailFooterIgnore = _localizer["EmailFooterIgnore"].Value
            };

            var fallbackSubject = _localizer["AccountLockedEmailSubject"].Value;
            var fallbackBody = _localizer["AccountLockedEmailFallback", lockoutMinutes].Value;

            var loadedTemplate = await _emailTemplateHelper.LoadTemplateAsync("AccountLocked", "AccountLocked", user.Email);
            var emailSubject = await _emailTemplateHelper.RenderSubjectAsync(loadedTemplate.Subject, templateModel, fallbackSubject);
            var emailBody = await _emailTemplateHelper.RenderBodyAsync(loadedTemplate.Body, templateModel, fallbackBody);

            await _notificationService.SendAsync(new NotificationMessage
            {
                Recipient = user.Email,
                Title = emailSubject,
                Body = emailBody,
                Channel = NotificationChannel.Email
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send account locked notification to {user.Email}: {ex.Message}");
        }
    }

    private string GetDashboardUrl() => $"{_configuration["FrontendUrl"] ?? "http://localhost:5173"}/dashboard";
    private string GetForgotPasswordUrl() => $"{_configuration["FrontendUrl"] ?? "http://localhost:5173"}/auth/forgot-password";
    private string GetResetPasswordUrl(string email, string code) => $"{_configuration["FrontendUrl"] ?? "http://localhost:5173"}/auth/reset-password?email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(code)}";

    private string FormatEventDateTime(DateTime occurredAtUtc)
    {
        return occurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
    }

    private string ResolveLocation(string? ipAddress)
    {
        return string.IsNullOrWhiteSpace(ipAddress)
            ? _localizer["SecurityUnknownLocation"].Value
            : ipAddress.Trim();
    }

    private string ResolveDevice(string? userAgent)
    {
        return string.IsNullOrWhiteSpace(userAgent)
            ? _localizer["SecurityUnknownDevice"].Value
            : userAgent.Trim();
    }

    private string? GetCurrentIpAddress()
        => RequestContextInfoResolver.ResolveClientIpAddress(_httpContextAccessor.HttpContext);

    private string? GetCurrentUserAgent()
    {
        return RequestContextInfoResolver.ResolveUserAgent(_httpContextAccessor.HttpContext);
    }
}
