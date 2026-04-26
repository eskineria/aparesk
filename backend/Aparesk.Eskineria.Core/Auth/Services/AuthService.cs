using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Auth.Constants;
using Aparesk.Eskineria.Core.Auth.Entities;
using Aparesk.Eskineria.Core.Auth.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Aparesk.Eskineria.Core.Notifications.Abstractions;
using Aparesk.Eskineria.Core.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Aparesk.Eskineria.Core.Auth.Utilities;

namespace Aparesk.Eskineria.Core.Auth.Services;

public class AuthService : IAuthService
{
    private const string AdminRoleName = Permissions.AdminRole;
    private const string DefaultRoleConfigKey = "Authentication:DefaultRole";
    private const string FallbackDefaultRoleName = Permissions.UserRole;
    private const string GoogleLoginProvider = "Google";
    private const string GoogleLoginDisplayName = "Google";
    private const int EmailVerificationMaxFailedAttempts = 5;
    private const int MinEmailVerificationCodeExpirySeconds = 30;
    private const int MaxEmailVerificationCodeExpirySeconds = 1800;
    private const int MinEmailVerificationResendCooldownSeconds = 5;
    private const int MaxEmailVerificationResendCooldownSeconds = 600;
    private const int MinPasswordMinLength = 6;
    private const int MaxPasswordMinLength = 128;
    private const int MinLoginMaxFailedAttempts = 3;
    private const int MaxLoginMaxFailedAttempts = 20;
    private const int MinLoginLockoutDurationMinutes = 1;
    private const int MaxLoginLockoutDurationMinutes = 1440;

    private readonly UserManager<EskineriaUser> _userManager;
    private readonly RoleManager<EskineriaRole> _roleManager;
    private readonly SignInManager<EskineriaUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly INotificationService _notificationService;
    private readonly AuthEmailTemplateHelper _emailTemplateHelper;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<AuthService> _localizer;
    private readonly IRoleSelectionAuditStore _roleSelectionAuditStore;
    private readonly IAccountService _accountService;

    public AuthService(
        UserManager<EskineriaUser> userManager,
        RoleManager<EskineriaRole> roleManager,
        SignInManager<EskineriaUser> signInManager,
        ITokenService tokenService,
        INotificationService notificationService,
        AuthEmailTemplateHelper emailTemplateHelper,
        IConfiguration configuration,
        IStringLocalizer<AuthService> localizer,
        IRoleSelectionAuditStore roleSelectionAuditStore,
        IAccountService accountService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _notificationService = notificationService;
        _emailTemplateHelper = emailTemplateHelper;
        _configuration = configuration;
        _localizer = localizer;
        _roleSelectionAuditStore = roleSelectionAuditStore;
        _accountService = accountService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, AuthRuntimeSettings runtimeSettings)
    {
        var normalizedSettings = NormalizeRuntimeSettings(runtimeSettings);
        ApplyPasswordPolicyToIdentityOptions(normalizedSettings);

        if (normalizedSettings.RegistrationInvitationRequired)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["RegistrationInvitationRequired"]
            };
        }

        var emailDomain = AccessPolicyEvaluator.GetEmailDomain(request.Email);
        if (string.IsNullOrWhiteSpace(emailDomain))
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["InvalidRequest"]
            };
        }

        var blockedDomains = AccessPolicyEvaluator.ParseList(normalizedSettings.RegistrationBlockedEmailDomains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (blockedDomains.Contains(emailDomain))
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["RegistrationDomainBlocked"]
            };
        }

        var allowedDomains = AccessPolicyEvaluator.ParseList(normalizedSettings.RegistrationAllowedEmailDomains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (allowedDomains.Count > 0 && !allowedDomains.Contains(emailDomain))
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["RegistrationDomainNotAllowed"]
            };
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["EmailAlreadyInUse"],
                Errors = new[] { _localizer["UserWithEmailExists"].Value }
            };
        }

        var newUser = new EskineriaUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = normalizedSettings.RegistrationAutoApproveEnabled,
            EmailConfirmed = !normalizedSettings.EmailVerificationRequired,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var createdForUser = await _userManager.CreateAsync(newUser, request.Password);
        if (!createdForUser.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["RegistrationFailed"],
                Errors = createdForUser.Errors.Select(x => x.Description)
            };
        }

        var ensureRoleResult = await EnsureUserHasRoleAsync(newUser);
        if (!ensureRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(newUser);
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["RoleAssignmentFailed"],
                Errors = ensureRoleResult.Errors.Select(x => x.Description)
            };
        }

        if (normalizedSettings.EmailVerificationRequired)
        {
            var emailResult = await SendEmailVerificationCodeAsync(newUser, normalizedSettings);
            if (!emailResult.Success)
            {
                await _userManager.DeleteAsync(newUser);
                return new AuthResponse
                {
                    Success = false,
                    Message = _localizer["SmtpDeliveryFailedError"] ?? "E-posta gönderilemedi. Lütfen ayarlarınızı kontrol edin."
                };
            }

            if (!normalizedSettings.RegistrationAutoApproveEnabled)
            {
                return new AuthResponse
                {
                    Success = true,
                    Message = _localizer["RegistrationSubmittedPendingApprovalConfirmEmail"]
                };
            }

            return new AuthResponse
            {
                Success = true,
                Message = _localizer["RegistrationSuccessfulConfirmEmail"]
            };
        }

        if (!normalizedSettings.RegistrationAutoApproveEnabled)
        {
            return new AuthResponse
            {
                Success = true,
                Message = _localizer["RegistrationSubmittedPendingApproval"]
            };
        }

        // Send welcome email immediately if verification is not required
        await _accountService.TrySendWelcomeEmailAsync(newUser);

        var tokenResponse = await _tokenService.GenerateTokensAsync(
            newUser,
            normalizedSettings.SessionAccessTokenLifetimeMinutes,
            normalizedSettings.SessionRefreshTokenLifetimeDays,
            normalizedSettings.SessionMaxActiveSessions);
        return new AuthResponse
        {
            Success = true,
            Message = _localizer["RegistrationSuccessful"],
            Data = tokenResponse
        };
    }

    public async Task<AuthResponse> VerifyEmailCodeAsync(VerifyEmailCodeRequest request, AuthRuntimeSettings runtimeSettings)
    {
        _ = runtimeSettings;
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = _localizer["InvalidRequest"] };
        }

        if (user.EmailConfirmed)
        {
            return new AuthResponse { Success = false, Message = _localizer["EmailAlreadyConfirmed"] };
        }

        if (string.IsNullOrWhiteSpace(user.EmailVerificationCodeHash) ||
            !user.EmailVerificationCodeExpiresAtUtc.HasValue)
        {
            return new AuthResponse { Success = false, Message = _localizer["EmailVerificationCodeExpired"] };
        }

        if (user.EmailVerificationCodeExpiresAtUtc.Value < DateTime.UtcNow)
        {
            ClearEmailVerificationState(user);
            await _userManager.UpdateAsync(user);
            return new AuthResponse { Success = false, Message = _localizer["EmailVerificationCodeExpired"] };
        }

        if (user.EmailVerificationFailedAttempts >= EmailVerificationMaxFailedAttempts)
        {
            ClearEmailVerificationState(user);
            await _userManager.UpdateAsync(user);
            return new AuthResponse { Success = false, Message = _localizer["EmailVerificationTooManyAttempts"] };
        }

        if (!VerificationCodeHelper.IsCodeValid(user.EmailVerificationCodeHash, user.SecurityStamp, request.Code))
        {
            user.EmailVerificationFailedAttempts += 1;

            if (user.EmailVerificationFailedAttempts >= EmailVerificationMaxFailedAttempts)
            {
                ClearEmailVerificationState(user);
                await _userManager.UpdateAsync(user);
                return new AuthResponse { Success = false, Message = _localizer["EmailVerificationTooManyAttempts"] };
            }

            await _userManager.UpdateAsync(user);
            return new AuthResponse { Success = false, Message = _localizer["EmailVerificationCodeInvalid"] };
        }

        user.EmailConfirmed = true;
        ClearEmailVerificationState(user);

        var updated = await _userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["EmailConfirmationFailed"],
                Errors = updated.Errors.Select(x => x.Description)
            };
        }

        await _userManager.UpdateSecurityStampAsync(user);

        // Send welcome email after successful verification
        await _accountService.TrySendWelcomeEmailAsync(user);

        return new AuthResponse { Success = true, Message = _localizer["EmailConfirmedSuccessfully"] };
    }

    public async Task<AuthResponse> ResendEmailVerificationCodeAsync(ResendEmailVerificationCodeRequest request, AuthRuntimeSettings runtimeSettings)
    {
        var normalizedSettings = NormalizeRuntimeSettings(runtimeSettings);
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Avoid user enumeration.
        if (user == null)
        {
            return new AuthResponse { Success = true, Message = _localizer["EmailVerificationCodeSentGeneric"] };
        }

        if (user.EmailConfirmed)
        {
            return new AuthResponse { Success = false, Message = _localizer["EmailAlreadyConfirmed"] };
        }

        if (user.EmailVerificationCodeSentAtUtc.HasValue)
        {
            var resendAt = user.EmailVerificationCodeSentAtUtc.Value.AddSeconds(normalizedSettings.EmailVerificationResendCooldownSeconds);
            if (resendAt > DateTime.UtcNow)
            {
                var secondsLeft = (int)Math.Ceiling((resendAt - DateTime.UtcNow).TotalSeconds);
                return new AuthResponse
                {
                    Success = false,
                    Message = _localizer["EmailVerificationResendTooSoon", secondsLeft]
                };
            }
        }

        var emailResult = await SendEmailVerificationCodeAsync(user, normalizedSettings, isResend: true);
        if (!emailResult.Success)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["SmtpDeliveryFailedError"] ?? "E-posta gönderimi başarısız oldu."
            };
        }
        return new AuthResponse { Success = true, Message = _localizer["EmailVerificationCodeResent"] };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, AuthRuntimeSettings runtimeSettings)
    {
        var normalizedSettings = NormalizeRuntimeSettings(runtimeSettings);
        ApplyLockoutPolicyToIdentityOptions(normalizedSettings);
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = _localizer["InvalidEmailOrPassword"] };
        }

        if (!user.IsActive)
        {
            return new AuthResponse { Success = false, Message = _localizer["UserIsInactive"] };
        }

        if (normalizedSettings.EmailVerificationRequired && !user.EmailConfirmed)
        {
            var hasSentFreshCode = await EnsureLoginVerificationCodeAsync(user, normalizedSettings);
            return new AuthResponse
            {
                Success = false,
                Message = hasSentFreshCode
                    ? _localizer["ConfirmEmailBeforeLoginCodeSent"]
                    : _localizer["ConfirmEmailBeforeLogin"],
                Errors = new[] { "EMAIL_NOT_CONFIRMED" }
            };
        }

        var roleResult = await EnsureUserHasRoleAsync(user);
        if (!roleResult.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["RoleAssignmentFailed"],
                Errors = roleResult.Errors.Select(x => x.Description)
            };
        }

        if (user.LockoutEnabled != normalizedSettings.LoginLockoutEnabled)
        {
            await _userManager.SetLockoutEnabledAsync(user, normalizedSettings.LoginLockoutEnabled);
        }

        if (!normalizedSettings.LoginLockoutEnabled)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        if (normalizedSettings.LoginLockoutEnabled && await _userManager.IsLockedOutAsync(user))
        {
             return new AuthResponse { Success = false, Message = _localizer["user_locked_out"] };
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: normalizedSettings.LoginLockoutEnabled);

        if (result.IsLockedOut)
        {
            await _accountService.TrySendAccountLockedNotificationAsync(user, (int)(normalizedSettings.LoginLockoutDurationMinutes));
            return new AuthResponse { Success = false, Message = _localizer["user_locked_out"] };
        }

        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var mfaBypassed = AccessPolicyEvaluator.IsIpAllowed(
                normalizedSettings.RequestIpAddress,
                normalizedSettings.MfaBypassIpWhitelist);

            var requiresMfa =
                normalizedSettings.MfaFeatureEnabled &&
                normalizedSettings.MfaEnforcedForAll;

            if (requiresMfa && !user.TwoFactorEnabled && !mfaBypassed)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = _localizer["MfaSetupRequired"],
                    Errors = new[] { "MFA_SETUP_REQUIRED" }
                };
            }

            if (normalizedSettings.MfaFeatureEnabled && user.TwoFactorEnabled && !mfaBypassed)
            {
                var providedCode = request.MfaCode?.Trim();
                if (string.IsNullOrWhiteSpace(providedCode))
                {
                    var mfaDeliveryResult = await SendMfaCodeAsync(user);
                    if (!mfaDeliveryResult.Success)
                    {
                        return new AuthResponse
                        {
                            Success = false,
                            Message = _localizer["MfaCodeDeliveryFailed"],
                            Errors = new[] { "MFA_DELIVERY_FAILED" }
                        };
                    }

                    return new AuthResponse
                    {
                        Success = false,
                        Message = _localizer["MfaCodeSent"],
                        Errors = new[] { "MFA_REQUIRED" }
                    };
                }

                var isMfaCodeValid = await _userManager.VerifyTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider,
                    providedCode);

                if (!isMfaCodeValid)
                {
                    await _userManager.AccessFailedAsync(user);
                    if (await _userManager.IsLockedOutAsync(user))
                    {
                        await _accountService.TrySendAccountLockedNotificationAsync(user, (int)(normalizedSettings.LoginLockoutDurationMinutes));
                        return new AuthResponse
                        {
                            Success = false,
                            Message = _localizer["AccountLocked"],
                            Errors = new[] { "LOCKED_OUT" }
                        };
                    }

                    return new AuthResponse
                    {
                        Success = false,
                        Message = _localizer["MfaCodeInvalid"],
                        Errors = new[] { "MFA_INVALID" }
                    };
                }

                await _userManager.ResetAccessFailedCountAsync(user);
            }

            if (roles.Count > 0 &&
                (string.IsNullOrWhiteSpace(user.ActiveRole) ||
                 !roles.Contains(user.ActiveRole, StringComparer.OrdinalIgnoreCase)))
            {
                user.ActiveRole = roles.OrderBy(r => r).First();
                await _userManager.UpdateAsync(user);
            }

            // Reset access failed count is handled automatically by SignInManager on success
            var tokenResponse = await _tokenService.GenerateTokensAsync(
                user,
                normalizedSettings.SessionAccessTokenLifetimeMinutes,
                normalizedSettings.SessionRefreshTokenLifetimeDays,
                normalizedSettings.SessionMaxActiveSessions);
            return new AuthResponse
            {
                Success = true,
                Data = tokenResponse
            };
        }

        return new AuthResponse { Success = false, Message = _localizer["InvalidEmailOrPassword"] };
    }

    public Task<AuthResponse> SocialLoginAsync(SocialLoginRequest request, AuthRuntimeSettings runtimeSettings)
    {
        if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.IdToken))
        {
            return Task.FromResult(new AuthResponse { Success = false, Message = _localizer["InvalidRequest"] });
        }

        return request.Provider.ToLowerInvariant() switch
        {
            "google" => LoginWithGoogleAsync(request.IdToken, request.MfaCode, runtimeSettings),
            _ => Task.FromResult(new AuthResponse { Success = false, Message = _localizer["SocialProviderNotSupported"] })
        };
    }

    public async Task<AuthResponse<RoleSwitchResultDto>> SwitchRoleAsync(Guid userId, string roleName, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return new AuthResponse<RoleSwitchResultDto> { Success = false, Message = _localizer[AuthLocalizationKeys.RoleNameRequired] };
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return new AuthResponse<RoleSwitchResultDto> { Success = false, Message = _localizer[AuthLocalizationKeys.UserNotFound] };
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            return new AuthResponse<RoleSwitchResultDto> { Success = false, Message = _localizer[AuthLocalizationKeys.UserDoesNotHaveSpecifiedRole] };
        }

        var previousRole = user.ActiveRole;
        user.ActiveRole = roleName;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var message = string.Join("; ", updateResult.Errors.Select(error => error.Description));
            return new AuthResponse<RoleSwitchResultDto> { Success = false, Message = message };
        }

        await _roleSelectionAuditStore.RecordSelectionAsync(
            user.Id,
            previousRole,
            roleName,
            DateTime.UtcNow,
            ipAddress,
            userAgent);

        return new AuthResponse<RoleSwitchResultDto>
        {
            Success = true,
            Message = _localizer[AuthLocalizationKeys.RoleSwitchedSuccessfully],
            Data = new RoleSwitchResultDto
            {
                ActiveRole = roleName,
                Roles = roles.OrderBy(role => role).ToList()
            }
        };
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _tokenService.RevokeRefreshTokenAsync(refreshToken);
        }
    }

    private async Task<NotificationResult> SendMfaCodeAsync(EskineriaUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("User email is required for MFA verification.");
        }

        var verificationCode = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

        var templateModel = new
        {
            MfaVerificationEmailTitle = _localizer["MfaVerificationEmailSubject"].Value,
            Greeting = _localizer["EmailGreeting", user.FirstName ?? string.Empty].Value,
            MfaVerificationEmailBody = _localizer["MfaVerificationEmailBody", string.Empty].Value.Replace(": ", "").Trim(), // Clean up placeholder if exists in string
            VerificationCodeLabel = _localizer["VerificationCodeLabel"].Value,
            VerificationCode = verificationCode,
            EmailSecurityNote = _localizer["EmailSecurityNote"].Value,
            EmailTeam = _localizer["EmailTeam"].Value,
            EmailFooterIgnore = _localizer["EmailFooterIgnore"].Value
        };

        var fallbackSubject = _localizer["MfaVerificationEmailSubject"].Value;
        var fallbackBody = _localizer["MfaVerificationEmailBody", verificationCode].Value;

        var loadedTemplate = await _emailTemplateHelper.LoadTemplateAsync("MfaLoginCode", "MfaLoginCode", user.Email);
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

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, AuthRuntimeSettings? runtimeSettings = null)
    {
        var normalizedSettings = NormalizeRuntimeSettings(runtimeSettings ?? new AuthRuntimeSettings());
        try
        {
            var tokenResponse = await _tokenService.RefreshTokenAsync(
                request.Token,
                request.RefreshToken,
                normalizedSettings.SessionAccessTokenLifetimeMinutes,
                normalizedSettings.SessionRefreshTokenLifetimeDays,
                normalizedSettings.SessionMaxActiveSessions);
            return new AuthResponse
            {
                Success = true,
                Data = tokenResponse
            };
        }
        catch (Exception)
        {
            return new AuthResponse
            {
                Success = false,
                Message = _localizer["TokenRefreshFailed"],
                Errors = Array.Empty<string>()
            };
        }
    }

    private async Task<NotificationResult> SendEmailVerificationCodeAsync(EskineriaUser user, AuthRuntimeSettings runtimeSettings, bool isResend = false)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("User email is required for email verification.");
        }

        var verificationCode = VerificationCodeHelper.GenerateCode();
        var now = DateTime.UtcNow;

        user.EmailVerificationCodeHash = VerificationCodeHelper.HashCode(verificationCode, user.SecurityStamp);
        user.EmailVerificationCodeExpiresAtUtc = now.AddSeconds(runtimeSettings.EmailVerificationCodeExpirySeconds);
        user.EmailVerificationCodeSentAtUtc = now;
        user.EmailVerificationFailedAttempts = 0;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException("Could not persist email verification code for the user.");
        }

        var templateModel = new
        {
            VerificationCodeEmailTitle = _localizer["VerificationCodeEmailTitle"].Value,
            Greeting = _localizer["EmailGreeting", user.FirstName ?? string.Empty].Value,
            VerificationCodeEmailContent = _localizer["VerificationCodeEmailContent"].Value,
            VerificationCodeLabel = _localizer["VerificationCodeLabel"].Value,
            VerificationCode = verificationCode,
            EmailSecurityNote = _localizer["EmailSecurityNote"].Value,
            VerificationCodeEmailExpiry = _localizer["VerificationCodeEmailExpiry", runtimeSettings.EmailVerificationCodeExpirySeconds].Value,
            EmailTeam = _localizer["EmailTeam"].Value,
            EmailFooterIgnore = _localizer["EmailFooterIgnore"].Value,
            IsResend = isResend
        };

        var fallbackSubject = _localizer["VerificationCodeEmailSubject"].Value;
        var fallbackBody = _localizer["EmailVerificationCodeFallback", verificationCode, runtimeSettings.EmailVerificationCodeExpirySeconds].Value;
        var loadedTemplate = await _emailTemplateHelper.LoadTemplateAsync("VerifyEmailCode", "VerifyEmailCode", user.Email);
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

    private async Task<bool> EnsureLoginVerificationCodeAsync(EskineriaUser user, AuthRuntimeSettings runtimeSettings)
    {
        var now = DateTime.UtcNow;
        var hasActiveCode =
            !string.IsNullOrWhiteSpace(user.EmailVerificationCodeHash) &&
            user.EmailVerificationCodeExpiresAtUtc.HasValue &&
            user.EmailVerificationCodeExpiresAtUtc.Value > now;

        if (hasActiveCode)
        {
            return false;
        }

        if (user.EmailVerificationCodeSentAtUtc.HasValue)
        {
            var resendAt = user.EmailVerificationCodeSentAtUtc.Value.AddSeconds(runtimeSettings.EmailVerificationResendCooldownSeconds);
            if (resendAt > now)
            {
                return false;
            }
        }

        await SendEmailVerificationCodeAsync(user, runtimeSettings, isResend: true);
        return true;
    }

    private static void ClearEmailVerificationState(EskineriaUser user)
    {
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationCodeExpiresAtUtc = null;
        user.EmailVerificationCodeSentAtUtc = null;
        user.EmailVerificationFailedAttempts = 0;
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
        settings.LoginMaxFailedAttempts = Math.Clamp(
            settings.LoginMaxFailedAttempts,
            MinLoginMaxFailedAttempts,
            MaxLoginMaxFailedAttempts);
        settings.LoginLockoutDurationMinutes = Math.Clamp(
            settings.LoginLockoutDurationMinutes,
            MinLoginLockoutDurationMinutes,
            MaxLoginLockoutDurationMinutes);
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

    private void ApplyLockoutPolicyToIdentityOptions(AuthRuntimeSettings settings)
    {
        _userManager.Options.Lockout.MaxFailedAccessAttempts = settings.LoginMaxFailedAttempts;
        _userManager.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(settings.LoginLockoutDurationMinutes);
        _userManager.Options.Lockout.AllowedForNewUsers = settings.LoginLockoutEnabled;
    }

    private async Task<IdentityResult> EnsureUserHasRoleAsync(EskineriaUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var defaultRole = _configuration[DefaultRoleConfigKey] ?? FallbackDefaultRoleName;
        defaultRole = defaultRole.Trim();
        if (string.IsNullOrWhiteSpace(defaultRole))
        {
            defaultRole = FallbackDefaultRoleName;
        }

        if (roles.Count > 0)
        {
            // Even if user has roles, ensure the active role has the base DashboardView permission
            var activeRoleName = user.ActiveRole ?? roles[0];
            await EnsureRoleHasBasePermissionsAsync(activeRoleName);
            return IdentityResult.Success;
        }

        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            var createRoleResult = await _roleManager.CreateAsync(new EskineriaRole(defaultRole));
            if (!createRoleResult.Succeeded)
            {
                return createRoleResult;
            }
        }

        await EnsureRoleHasBasePermissionsAsync(defaultRole);

        var addToRoleResult = await _userManager.AddToRoleAsync(user, defaultRole);
        if (!addToRoleResult.Succeeded)
        {
            return addToRoleResult;
        }

        if (string.IsNullOrWhiteSpace(user.ActiveRole))
        {
            user.ActiveRole = defaultRole;
            return await _userManager.UpdateAsync(user);
        }

        return IdentityResult.Success;
    }

    private async Task EnsureRoleHasBasePermissionsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return;

        var claims = await _roleManager.GetClaimsAsync(role);
        var basePermissions = new[] { Permissions.DashboardView };

        foreach (var permission in basePermissions)
        {
            if (!claims.Any(c => c.Type == Permissions.ClaimType && c.Value == permission))
            {
                await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(Permissions.ClaimType, permission));
            }
        }
    }

    private async Task<AuthResponse> LoginWithGoogleAsync(string idToken, string? mfaCode, AuthRuntimeSettings runtimeSettings)
    {
        try
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId) ||
                clientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ||
                clientId.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase))
            {
                return new AuthResponse { Success = false, Message = _localizer["SocialGoogleNotConfigured"] };
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            var providerKey = payload.Subject;
            if (string.IsNullOrWhiteSpace(providerKey))
            {
                return new AuthResponse { Success = false, Message = _localizer["SocialGoogleIdentifierMissing"] };
            }

            var user = await _userManager.FindByLoginAsync(GoogleLoginProvider, providerKey);
            if (user == null && !string.IsNullOrWhiteSpace(payload.Email))
            {
                user = await _userManager.FindByEmailAsync(payload.Email);
            }

            if (user == null)
            {
                if (string.IsNullOrWhiteSpace(payload.Email))
                {
                    return new AuthResponse { Success = false, Message = _localizer["SocialGoogleEmailMissing"] };
                }

                if (runtimeSettings.RegistrationInvitationRequired)
                {
                    return new AuthResponse { Success = false, Message = _localizer["RegistrationInvitationRequired"] };
                }

                var emailDomain = AccessPolicyEvaluator.GetEmailDomain(payload.Email);
                if (string.IsNullOrWhiteSpace(emailDomain))
                {
                    return new AuthResponse { Success = false, Message = _localizer["InvalidRequest"] };
                }

                var blockedDomains = AccessPolicyEvaluator.ParseList(runtimeSettings.RegistrationBlockedEmailDomains)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (blockedDomains.Contains(emailDomain))
                {
                    return new AuthResponse { Success = false, Message = _localizer["RegistrationDomainBlocked"] };
                }

                var allowedDomains = AccessPolicyEvaluator.ParseList(runtimeSettings.RegistrationAllowedEmailDomains)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (allowedDomains.Count > 0 && !allowedDomains.Contains(emailDomain))
                {
                    return new AuthResponse { Success = false, Message = _localizer["RegistrationDomainNotAllowed"] };
                }

                user = new EskineriaUser
                {
                    Email = payload.Email,
                    UserName = payload.Email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    EmailConfirmed = true,
                    IsActive = runtimeSettings.RegistrationAutoApproveEnabled
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = _localizer["SocialGoogleCreateUserFailed"],
                        Errors = result.Errors.Select(e => e.Description)
                    };
                }

                if (!runtimeSettings.RegistrationAutoApproveEnabled)
                {
                    return new AuthResponse { Success = false, Message = _localizer["RegistrationSubmittedPendingApproval"] };
                }
            }
            else if (!user.IsActive)
            {
                return new AuthResponse { Success = false, Message = _localizer["UserIsInactive"] };
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var confirmUpdate = await _userManager.UpdateAsync(user);
                if (!confirmUpdate.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = _localizer["SocialGoogleConfirmationUpdateFailed"],
                        Errors = confirmUpdate.Errors.Select(e => e.Description)
                    };
                }
            }

            var linkResult = await EnsureGoogleLoginLinkAsync(user, providerKey);
            if (!linkResult.Success)
            {
                return linkResult;
            }

            var roleResult = await EnsureUserHasRoleAsync(user);
            if (!roleResult.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = _localizer["RoleAssignmentFailed"],
                    Errors = roleResult.Errors.Select(e => e.Description)
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count > 0 &&
                (string.IsNullOrWhiteSpace(user.ActiveRole) ||
                 !roles.Contains(user.ActiveRole, StringComparer.OrdinalIgnoreCase)))
            {
                user.ActiveRole = roles.OrderBy(r => r).First();
                await _userManager.UpdateAsync(user);
            }

            // MFA is bypassed for Google logins as requested
            // Google accounts usually have their own MFA and we trust the provider verification.
            
            var tokenResponse = await _tokenService.GenerateTokensAsync(
                user,
                runtimeSettings.SessionAccessTokenLifetimeMinutes,
                runtimeSettings.SessionRefreshTokenLifetimeDays,
                runtimeSettings.SessionMaxActiveSessions);

            return new AuthResponse { Success = true, Message = _localizer["LoginSuccessful"], Data = tokenResponse };
        }
        catch (Exception)
        {
            return new AuthResponse { Success = false, Message = _localizer["SocialGoogleAuthenticationFailed"] };
        }
    }

    private async Task<AuthResponse> EnsureGoogleLoginLinkAsync(EskineriaUser user, string providerKey)
    {
        var existingLogins = await _userManager.GetLoginsAsync(user);
        var hasGoogleLogin = existingLogins.Any(login =>
            string.Equals(login.LoginProvider, GoogleLoginProvider, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(login.ProviderKey, providerKey, StringComparison.Ordinal));

        if (hasGoogleLogin)
        {
            return new AuthResponse { Success = true };
        }

        var addLoginResult = await _userManager.AddLoginAsync(
            user,
            new UserLoginInfo(GoogleLoginProvider, providerKey, GoogleLoginDisplayName));

        if (addLoginResult.Succeeded)
        {
            return new AuthResponse { Success = true };
        }

        return new AuthResponse
        {
            Success = false,
            Message = _localizer["SocialGoogleLinkFailed"],
            Errors = addLoginResult.Errors.Select(e => e.Description)
        };
    }
}
