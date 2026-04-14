using System.Security.Claims;
using Eskineria.Core.Auth.Models;

namespace Eskineria.Core.Auth.Abstractions;

public interface IAccountService
{
    Task<AuthResponse> ChangePasswordAsync(ClaimsPrincipal user, ChangePasswordRequest request, AuthRuntimeSettings? runtimeSettings = null);
    Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request, AuthRuntimeSettings? runtimeSettings = null);
    Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request, AuthRuntimeSettings? runtimeSettings = null);
    Task<AuthResponse> VerifyPasswordResetCodeAsync(VerifyPasswordResetCodeRequest request);
    Task<AuthResponse> ResendPasswordResetCodeAsync(ResendPasswordResetCodeRequest request, AuthRuntimeSettings? runtimeSettings = null);
    Task<AuthResponse> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<AuthResponse<UserInfoDto>> GetUserInfoAsync(ClaimsPrincipal user);
    Task<AuthResponse<List<UserSessionDto>>> GetUserSessionsAsync(ClaimsPrincipal user, string? currentRefreshToken);
    Task<AuthResponse> RevokeSessionAsync(ClaimsPrincipal user, Guid sessionId);
    Task<AuthResponse> RevokeOtherSessionsAsync(ClaimsPrincipal user, string? currentRefreshToken);
    Task<AuthResponse> UpdateUserInfoAsync(ClaimsPrincipal user, UpdateUserInfoRequest request);
    Task<AuthResponse<MfaStatusDto>> GetMfaStatusAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task SendLoginNotificationAsync(Guid userId, string? ipAddress, string? userAgent);
    Task<AuthResponse> SendMfaActionCodeAsync(ClaimsPrincipal user, bool targetState);
    Task TrySendWelcomeEmailAsync(Core.Auth.Entities.EskineriaUser user);
    Task TrySendAccountLockedNotificationAsync(Core.Auth.Entities.EskineriaUser user, int lockoutMinutes);
}
