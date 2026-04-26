namespace Aparesk.Eskineria.Core.RateLimit.Configuration;

public static class RateLimitPolicyNames
{
    public const string AuthLogin = "auth-login";
    public const string AuthRegister = "auth-register";
    public const string AuthPasswordRecovery = "auth-password-recovery";
    public const string AuthOtpVerification = "auth-otp-verification";
    public const string AuthOtpResend = "auth-otp-resend";
    public const string AuthTokenRefresh = "auth-token-refresh";
    public const string AuthAuthenticatedSensitive = "auth-authenticated-sensitive";
}
