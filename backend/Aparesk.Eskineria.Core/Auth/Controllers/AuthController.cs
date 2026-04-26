using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Auth.Constants;
using Aparesk.Eskineria.Core.Auth.Entities;
using Aparesk.Eskineria.Core.Auth.Models;
using Aparesk.Eskineria.Core.Compliance.Abstractions;
using Aparesk.Eskineria.Core.Compliance.Models;

using Aparesk.Eskineria.Core.Settings.Abstractions;
using Aparesk.Eskineria.Core.Shared.Configuration;
using Aparesk.Eskineria.Core.Shared.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Hosting;
using Aparesk.Eskineria.Core.Auditing.Abstractions;
using Aparesk.Eskineria.Core.Auditing.Models;
using System.Security.Claims;
using System.Text.Json;
using Aparesk.Eskineria.Core.Auth.Utilities;
using Aparesk.Eskineria.Core.RateLimit.Configuration;
using Microsoft.AspNetCore.RateLimiting;

namespace Aparesk.Eskineria.Core.Auth.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{


    private readonly IAuthService _authService;
    private readonly IAccountService _accountService;
    private readonly IComplianceService _complianceService;

    private readonly ISystemSettingsService _systemSettingsService;
    private readonly ITokenService _tokenService;
    private readonly UserManager<EskineriaUser> _userManager;
    private readonly IStringLocalizer<AuthController> _localizer;
    private readonly IHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IAccountService accountService,
        IComplianceService complianceService,
        ISystemSettingsService systemSettingsService,
        ITokenService tokenService,
        UserManager<EskineriaUser> userManager,
        IStringLocalizer<AuthController> localizer,
        IHostEnvironment environment)
    {
        _authService = authService;
        _accountService = accountService;
        _complianceService = complianceService;
        _systemSettingsService = systemSettingsService;
        _tokenService = tokenService;
        _userManager = userManager;
        _localizer = localizer;
        _environment = environment;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthRegister)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!await _systemSettingsService.IsRegistrationEnabledAsync())
        {
            var disabledResponse = new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.RegistrationIsDisabled]
            };

            return BadRequest(disabledResponse);
        }

        var termsOfService = await _complianceService.GetActiveTermsByTypeAsync("TermsOfService");
        if (termsOfService.Success && termsOfService.Data != null && !request.TermsAccepted)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer["TermsAcceptanceRequired"]
            });
        }

        var privacyPolicy = await _complianceService.GetActiveTermsByTypeAsync("PrivacyPolicy");
        if (privacyPolicy.Success && privacyPolicy.Data != null && !request.PrivacyPolicyAccepted)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer["TermsAcceptanceRequired"]
            });
        }

        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var response = await _authService.RegisterAsync(request, runtimeSettings);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        // Record required compliance acceptances captured during registration.
        if (request.TermsAccepted || request.PrivacyPolicyAccepted)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
            {
                var requestedTypes = new List<string>();
                if (request.TermsAccepted)
                {
                    requestedTypes.Add("TermsOfService");
                }

                if (request.PrivacyPolicyAccepted)
                {
                    requestedTypes.Add("PrivacyPolicy");
                }

                foreach (var termsType in requestedTypes)
                {
                    var activeTerms = await _complianceService.GetActiveTermsByTypeAsync(termsType);
                    if (!activeTerms.Success || activeTerms.Data == null)
                    {
                        continue;
                    }

                    await _complianceService.AcceptTermsAsync(
                        user.Id,
                        activeTerms.Data.Id,
                        GetRequestIpAddress(),
                        GetRequestUserAgent());
                }
            }
        }

        if (response.Data != null) SetTokenCookies(response);

        return Ok(response);
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthLogin)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var runtimeSettings = await GetAuthRuntimeSettingsAsync();

        if (runtimeSettings.MaintenanceModeEnabled)
        {
            var hasMaintenanceAccess = AccessPolicyEvaluator.IsIpAllowed(
                GetRequestIpAddress(),
                runtimeSettings.MaintenanceIpWhitelist);

            if (!hasMaintenanceAccess)
            {
                var candidateUser = await _userManager.FindByEmailAsync(request.Email);
                if (candidateUser != null)
                {
                    var candidateRoles = await _userManager.GetRolesAsync(candidateUser);
                    hasMaintenanceAccess = AccessPolicyEvaluator.IsRoleAllowed(
                        candidateRoles,
                        runtimeSettings.MaintenanceRoleWhitelist);
                }
            }

            if (!hasMaintenanceAccess)
            {
                var maintenanceResponse = new AuthResponse
                {
                    Success = false,
                    Message = _localizer[LocalizationKeys.MaintenanceModeEnabled]
                };

                return StatusCode(StatusCodes.Status503ServiceUnavailable, maintenanceResponse);
            }
        }

        if (!await _systemSettingsService.IsLoginEnabledAsync())
        {
            var disabledResponse = new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.LoginIsDisabled]
            };

            return BadRequest(disabledResponse);
        }

        var response = await _authService.LoginAsync(request, runtimeSettings);
        if (!response.Success)
        {
            return Unauthorized(response);
        }
        
        SetTokenCookies(response);
        var user = await _userManager.FindByEmailAsync(request.Email);
        SetActiveRoleCookie(user?.ActiveRole);
        if (user != null && runtimeSettings.NotificationLoginAlertEnabled)
        {
            await _accountService.SendLoginNotificationAsync(
                user.Id,
                GetRequestIpAddress(),
                GetRequestUserAgent());
        }

        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthTokenRefresh)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        var token = request?.Token ?? Request.Cookies["X-Access-Token"];
        var refreshToken = request?.RefreshToken ?? Request.Cookies["X-Refresh-Token"];

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
        {
             return BadRequest(new AuthResponse
             {
                 Success = false,
                 Message = _localizer[LocalizationKeys.RefreshTokenRequestRequired]
             });
        }

        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var response = await _authService.RefreshTokenAsync(
            new RefreshTokenRequest { Token = token, RefreshToken = refreshToken },
            runtimeSettings);
        if (!response.Success) return BadRequest(response);
        
        SetTokenCookies(response);
        return Ok(response);
    }

    [HttpPost("social-login")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthLogin)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SocialLogin([FromBody] SocialLoginRequest request)
    {
        if (!await _systemSettingsService.IsLoginEnabledAsync())
        {
            var disabledResponse = new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.LoginIsDisabled]
            };

            return BadRequest(disabledResponse);
        }

        if (string.Equals(request.Provider, "google", StringComparison.OrdinalIgnoreCase) &&
            !await _systemSettingsService.IsGoogleLoginEnabledAsync())
        {
            var disabledResponse = new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.GoogleLoginIsDisabled]
            };

            return BadRequest(disabledResponse);
        }

        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var response = await _authService.SocialLoginAsync(request, runtimeSettings);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        SetTokenCookies(response);
        var socialLoginUserId = await GetUserIdFromAccessTokenAsync(response.Data?.AccessToken);
        if (socialLoginUserId.HasValue && runtimeSettings.NotificationLoginAlertEnabled)
        {
            await _accountService.SendLoginNotificationAsync(
                socialLoginUserId.Value,
                GetRequestIpAddress(),
                GetRequestUserAgent());
        }
        return Ok(response);
    }

    [Authorize]
    [HttpGet("user-info")]
    [ProducesResponseType(typeof(AuthResponse<UserInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserInfo(CancellationToken cancellationToken)
    {
        var response = await _accountService.GetUserInfoAsync(User);
        if (!response.Success || response.Data == null) return Unauthorized(response);

        return Ok(response);
    }

    [Authorize]
    [HttpGet("mfa-status")]
    [ProducesResponseType(typeof(AuthResponse<MfaStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMfaStatus(CancellationToken cancellationToken)
    {
        var response = await _accountService.GetMfaStatusAsync(User, cancellationToken);
        if (!response.Success)
        {
            return Unauthorized(response);
        }

        if (response.Data == null)
        {
            response.Data = new MfaStatusDto();
        }

        return Ok(response);
    }

    [Authorize]
    [HttpPost("send-mfa-code")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMfaCode([FromBody] SendMfaCodeRequest request)
    {
        var response = await _accountService.SendMfaActionCodeAsync(User, request.TargetState);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("mfa")]
    [ProducesResponseType(typeof(AuthResponse<MfaStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMfa([FromBody] UpdateMfaRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.UserNotFound]
            });
        }

        // If a code is provided (usually for social login users), verify it first
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, request.Code);
            if (!isCodeValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = _localizer["MfaCodeInvalid"]
                });
            }
        }
        else if (await _userManager.HasPasswordAsync(user))
        {
            // If user has a password and no code was provided, current password is required
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = _localizer["InvalidRequest"]
                });
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isPasswordValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = _localizer["InvalidEmailOrPassword"]
                });
            }
        }
        else
        {
            // Identity could not be verified (no code for social user, or no password/code for others)
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer["InvalidRequest"]
            });
        }

        if (user.TwoFactorEnabled != request.Enabled)
        {
            user.TwoFactorEnabled = request.Enabled;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = _localizer["InvalidRequest"],
                    Errors = updateResult.Errors.Select(x => x.Description)
                });
            }

            await _userManager.UpdateSecurityStampAsync(user);
        }

        return Ok(new AuthResponse<MfaStatusDto>
        {
            Success = true,
            Message = _localizer[LocalizationKeys.MfaUpdated],
            Data = new MfaStatusDto
            {
                Enabled = user.TwoFactorEnabled
            }
        });
    }

    [Authorize]
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(AuthResponse<List<string>>), StatusCodes.Status200OK)]
    public IActionResult GetPermissions()
    {
        var permissions = User.Claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p)
            .ToList();

        return Ok(new AuthResponse<List<string>>
        {
            Success = true,
            Data = permissions
        });
    }

    [Authorize]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(AuthResponse<List<UserSessionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessions()
    {
        if (!await _systemSettingsService.IsSessionManagementEnabledAsync())
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.SessionManagementIsDisabled]
            });
        }

        var currentRefreshToken = Request.Cookies["X-Refresh-Token"];
        var response = await _accountService.GetUserSessionsAsync(User, currentRefreshToken);
        if (!response.Success) return Unauthorized(response);
        return Ok(response);
    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeSession([FromRoute] Guid sessionId)
    {
        if (!await _systemSettingsService.IsSessionManagementEnabledAsync())
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.SessionManagementIsDisabled]
            });
        }

        Guid? currentSessionId = null;
        var currentRefreshToken = Request.Cookies["X-Refresh-Token"];
        if (!string.IsNullOrWhiteSpace(currentRefreshToken))
        {
            currentSessionId = await _tokenService.GetSessionIdAsync(currentRefreshToken);
        }

        var response = await _accountService.RevokeSessionAsync(User, sessionId);
        if (!response.Success) return BadRequest(response);

        if (currentSessionId.HasValue && currentSessionId.Value == sessionId)
        {
            DeleteAuthCookies();
        }

        return Ok(response);
    }

    [Authorize]
    [HttpPost("sessions/revoke-others")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeOtherSessions()
    {
        if (!await _systemSettingsService.IsSessionManagementEnabledAsync())
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.SessionManagementIsDisabled]
            });
        }

        var currentRefreshToken = Request.Cookies["X-Refresh-Token"];
        var response = await _accountService.RevokeOtherSessionsAsync(User, currentRefreshToken);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("switch-role")]
    [ProducesResponseType(typeof(AuthResponse<RoleSwitchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SwitchRole([FromBody] SwitchRoleRequest request)
    {
        var userIdValue = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new AuthResponse { Success = false, Message = _localizer[LocalizationKeys.UserNotFound] });
        }

        var response = await _authService.SwitchRoleAsync(
            userId,
            request.RoleName,
            GetRequestIpAddress(),
            GetRequestUserAgent());

        if (!response.Success || response.Data == null)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = response.Message
            });
        }

        var user = await _userManager.FindByIdAsync(userIdValue);
        if (user == null)
        {
            return Unauthorized(new AuthResponse { Success = false, Message = _localizer[LocalizationKeys.UserNotFound] });
        }

        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var tokenResponse = await _tokenService.GenerateTokensAsync(
            user,
            runtimeSettings.SessionAccessTokenLifetimeMinutes,
            runtimeSettings.SessionRefreshTokenLifetimeDays,
            runtimeSettings.SessionMaxActiveSessions);
        var authResponse = new AuthResponse<RoleSwitchResultDto>
        {
            Success = true,
            Message = response.Message,
            Data = new RoleSwitchResultDto
            {
                ActiveRole = response.Data.ActiveRole,
                Roles = response.Data.Roles
            }
        };

        SetTokenCookies(new AuthResponse { Success = true, Data = tokenResponse });
        SetActiveRoleCookie(response.Data.ActiveRole);

        return Ok(authResponse);
    }

    [Authorize]
    [HttpPut("update-info")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserInfoRequest request)
    {
        var response = await _accountService.UpdateUserInfoAsync(User, request);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["X-Refresh-Token"];
        await _authService.LogoutAsync(refreshToken);

        DeleteAuthCookies();
        return Ok(new { success = true, message = _localizer[LocalizationKeys.LoggedOutSuccessfully].Value });
    }

    [Authorize]
    [HttpPost("change-password")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthAuthenticatedSensitive)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!await _systemSettingsService.IsChangePasswordEnabledAsync())
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.PasswordChangeIsDisabled]
            });
        }

        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var response = await _accountService.ChangePasswordAsync(User, request, runtimeSettings);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthPasswordRecovery)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!await _systemSettingsService.IsForgotPasswordEnabledAsync())
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.ForgotPasswordIsDisabled]
            });
        }

        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var response = await _accountService.ForgotPasswordAsync(request, runtimeSettings);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthPasswordRecovery)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!await _systemSettingsService.IsForgotPasswordEnabledAsync())
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.ForgotPasswordIsDisabled]
            });
        }

        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var response = await _accountService.ResetPasswordAsync(request, runtimeSettings);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("verify-reset-password-code")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthOtpVerification)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyResetPasswordCode([FromBody] VerifyPasswordResetCodeRequest request)
    {
        if (!await _systemSettingsService.IsForgotPasswordEnabledAsync())
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.ForgotPasswordIsDisabled]
            });
        }

        var response = await _accountService.VerifyPasswordResetCodeAsync(request);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("resend-reset-password-code")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthOtpResend)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendResetPasswordCode([FromBody] ResendPasswordResetCodeRequest request)
    {
        if (!await _systemSettingsService.IsForgotPasswordEnabledAsync())
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = _localizer[LocalizationKeys.ForgotPasswordIsDisabled]
            });
        }

        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var response = await _accountService.ResendPasswordResetCodeAsync(request, runtimeSettings);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("confirm-email")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthOtpVerification)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] VerifyEmailCodeRequest request)
    {
        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var response = await _authService.VerifyEmailCodeAsync(request, runtimeSettings);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("resend-confirmation-code")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthOtpResend)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmationCode([FromBody] ResendEmailVerificationCodeRequest request)
    {
        var runtimeSettings = await GetAuthRuntimeSettingsAsync();
        var response = await _authService.ResendEmailVerificationCodeAsync(request, runtimeSettings);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    private void SetTokenCookies(AuthResponse response)
    {
        if (response.Data == null) return;

        Response.Cookies.Append(
            "X-Access-Token",
            response.Data.AccessToken,
            CreateAccessTokenCookieOptions(response.Data.ExpiryDate));

        Response.Cookies.Append(
            "X-Refresh-Token",
            response.Data.RefreshToken,
            CreateRefreshTokenCookieOptions(response.Data.RefreshTokenExpiryDate));
    }

    private void SetActiveRoleCookie(string? activeRole)
    {
        if (string.IsNullOrWhiteSpace(activeRole)) return;

        Response.Cookies.Append("X-Active-Role", activeRole, CreateActiveRoleCookieOptions());
    }

    private void DeleteAuthCookies()
    {
        Response.Cookies.Delete("X-Access-Token", CreateAccessTokenCookieOptions(DateTime.UtcNow.AddDays(-1)));
        Response.Cookies.Delete("X-Refresh-Token", CreateRefreshTokenCookieOptions(DateTime.UtcNow.AddDays(-1)));
        Response.Cookies.Delete("X-Active-Role", CreateActiveRoleCookieOptions());
    }

    private CookieOptions CreateAccessTokenCookieOptions(DateTime expiresAtUtc)
    {
        // Add a small buffer (5 min) to the cookie expiration to ensure the cookie stays 
        // until the token validation itself (backend) decides it's expired.
        var expires = expiresAtUtc.AddMinutes(5);
        var remaining = expires - DateTime.UtcNow;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

        var isLocalhost = Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || 
                          Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !isLocalhost && !_environment.IsDevelopment(),
            SameSite = isLocalhost ? SameSiteMode.Lax : SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            Expires = expires,
            MaxAge = remaining
        };
    }

    private CookieOptions CreateRefreshTokenCookieOptions(DateTime expiresAtUtc)
    {
        var expires = expiresAtUtc.AddMinutes(5);
        var remaining = expires - DateTime.UtcNow;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

        var isLocalhost = Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || 
                          Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !isLocalhost && !_environment.IsDevelopment(),
            SameSite = isLocalhost ? SameSiteMode.Lax : SameSiteMode.Strict,
            Path = "/api",
            IsEssential = true,
            Expires = expires,
            MaxAge = remaining
        };
    }

    private CookieOptions CreateActiveRoleCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = false,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTime.UtcNow.AddDays(7)
        };
    }

    private async Task<AuthRuntimeSettings> GetAuthRuntimeSettingsAsync()
    {
        var settingsResponse = await _systemSettingsService.GetAuthSettingsAsync();
        var settings = settingsResponse.Success ? settingsResponse.Data : null;

        if (settings == null)
        {
            var fallbackSessionMaxActiveSessions = await _systemSettingsService.GetSessionMaxActiveSessionsAsync();
            var fallbackSingleDeviceModeEnabled = await _systemSettingsService.IsSessionSingleDeviceModeEnabledAsync();

            return new AuthRuntimeSettings
            {
                EmailVerificationRequired = await _systemSettingsService.IsEmailVerificationRequiredAsync(),
                EmailVerificationCodeExpirySeconds = await _systemSettingsService.GetEmailVerificationCodeExpirySecondsAsync(),
                EmailVerificationResendCooldownSeconds = await _systemSettingsService.GetEmailVerificationResendCooldownSecondsAsync(),
                SessionAccessTokenLifetimeMinutes = await _systemSettingsService.GetSessionAccessTokenLifetimeMinutesAsync(),
                SessionRefreshTokenLifetimeDays = await _systemSettingsService.GetSessionRefreshTokenLifetimeDaysAsync(),
                SessionMaxActiveSessions = fallbackSingleDeviceModeEnabled ? 1 : fallbackSessionMaxActiveSessions,
                SessionIdleTimeoutMinutes = await _systemSettingsService.GetSessionIdleTimeoutMinutesAsync(),
                SessionWarningBeforeTimeoutMinutes = await _systemSettingsService.GetSessionWarningBeforeTimeoutMinutesAsync(),
                SessionRememberMeDurationDays = await _systemSettingsService.GetSessionRememberMeDurationDaysAsync(),
                SessionSingleDeviceModeEnabled = fallbackSingleDeviceModeEnabled,
                PasswordMinLength = await _systemSettingsService.GetPasswordMinLengthAsync(),
                PasswordRequireUppercase = await _systemSettingsService.IsPasswordRequireUppercaseAsync(),
                PasswordRequireLowercase = await _systemSettingsService.IsPasswordRequireLowercaseAsync(),
                PasswordRequireDigit = await _systemSettingsService.IsPasswordRequireDigitAsync(),
                PasswordRequireNonAlphanumeric = await _systemSettingsService.IsPasswordRequireNonAlphanumericAsync(),
                LoginLockoutEnabled = await _systemSettingsService.IsLoginLockoutEnabledAsync(),
                LoginMaxFailedAttempts = await _systemSettingsService.GetLoginMaxFailedAttemptsAsync(),
                LoginLockoutDurationMinutes = await _systemSettingsService.GetLoginLockoutDurationMinutesAsync(),
                MaintenanceModeEnabled = await _systemSettingsService.IsMaintenanceModeEnabledAsync(),
                NotificationLoginAlertEnabled = await _systemSettingsService.IsNotificationLoginAlertEnabledAsync(),
                MfaFeatureEnabled = await _systemSettingsService.IsMfaEnabledAsync(),
                RequestIpAddress = GetRequestIpAddress() ?? string.Empty,
            };
        }

        var singleDeviceModeEnabled = settings.SessionSingleDeviceModeEnabled;

        return new AuthRuntimeSettings
        {
            EmailVerificationRequired = settings.EmailVerificationRequired,
            EmailVerificationCodeExpirySeconds = settings.EmailVerificationCodeExpirySeconds,
            EmailVerificationResendCooldownSeconds = settings.EmailVerificationResendCooldownSeconds,
            SessionAccessTokenLifetimeMinutes = settings.SessionAccessTokenLifetimeMinutes,
            SessionRefreshTokenLifetimeDays = settings.SessionRefreshTokenLifetimeDays,
            SessionMaxActiveSessions = singleDeviceModeEnabled ? 1 : settings.SessionMaxActiveSessions,
            SessionIdleTimeoutMinutes = settings.SessionIdleTimeoutMinutes,
            SessionWarningBeforeTimeoutMinutes = settings.SessionWarningBeforeTimeoutMinutes,
            SessionRememberMeDurationDays = settings.SessionRememberMeDurationDays,
            SessionSingleDeviceModeEnabled = singleDeviceModeEnabled,
            PasswordMinLength = settings.PasswordMinLength,
            PasswordRequireUppercase = settings.PasswordRequireUppercase,
            PasswordRequireLowercase = settings.PasswordRequireLowercase,
            PasswordRequireDigit = settings.PasswordRequireDigit,
            PasswordRequireNonAlphanumeric = settings.PasswordRequireNonAlphanumeric,
            MfaEnforcedForAll = settings.MfaEnforcedForAll,
            MfaTrustedDeviceDurationDays = settings.MfaTrustedDeviceDurationDays,
            MfaBypassIpWhitelist = settings.MfaBypassIpWhitelist,
            RegistrationInvitationRequired = settings.RegistrationInvitationRequired,
            RegistrationAllowedEmailDomains = settings.RegistrationAllowedEmailDomains,
            RegistrationBlockedEmailDomains = settings.RegistrationBlockedEmailDomains,
            RegistrationAutoApproveEnabled = settings.RegistrationAutoApproveEnabled,
            LoginLockoutEnabled = settings.LoginLockoutEnabled,
            LoginMaxFailedAttempts = settings.LoginMaxFailedAttempts,
            LoginLockoutDurationMinutes = settings.LoginLockoutDurationMinutes,
            MaintenanceModeEnabled = settings.MaintenanceModeEnabled,
            MaintenanceIpWhitelist = settings.MaintenanceIpWhitelist,
            MaintenanceRoleWhitelist = settings.MaintenanceRoleWhitelist,
            NotificationLoginAlertEnabled = settings.NotificationLoginAlertEnabled,
            MfaFeatureEnabled = settings.MfaFeatureEnabled,
            RequestIpAddress = GetRequestIpAddress() ?? string.Empty,
        };
    }

    private string? GetRequestIpAddress()
        => RequestContextInfoResolver.ResolveClientIpAddress(HttpContext);

    private string? GetRequestUserAgent()
    {
        return RequestContextInfoResolver.ResolveUserAgent(HttpContext);
    }

    private async Task<Guid?> GetUserIdFromAccessTokenAsync(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var principal = await _tokenService.GetPrincipalFromTokenAsync(accessToken);
        if (principal == null)
        {
            return null;
        }

        var userIdValue = principal.FindFirstValue("id");
        return Guid.TryParse(userIdValue, out var parsed) ? parsed : null;
    }
}
