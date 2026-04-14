using Eskineria.Core.Auth.Models;

namespace Eskineria.Core.Auth.Abstractions;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, AuthRuntimeSettings runtimeSettings);
    Task<AuthResponse> LoginAsync(LoginRequest request, AuthRuntimeSettings runtimeSettings);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, AuthRuntimeSettings? runtimeSettings = null);
    Task<AuthResponse> VerifyEmailCodeAsync(VerifyEmailCodeRequest request, AuthRuntimeSettings runtimeSettings);
    Task<AuthResponse> ResendEmailVerificationCodeAsync(ResendEmailVerificationCodeRequest request, AuthRuntimeSettings runtimeSettings);
    Task<AuthResponse> SocialLoginAsync(SocialLoginRequest request, AuthRuntimeSettings runtimeSettings);
    Task<AuthResponse<RoleSwitchResultDto>> SwitchRoleAsync(Guid userId, string roleName, string? ipAddress, string? userAgent);
    Task LogoutAsync(string? refreshToken);
}
