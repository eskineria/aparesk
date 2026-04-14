export interface LoginRequest {
    email: string;
    password: string;
    mfaCode?: string;
}

export interface SocialLoginRequest {
    provider: string;
    idToken: string;
    mfaCode?: string;
}

export interface RegisterRequest {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    confirmPassword: string;
    termsAccepted: boolean;
    privacyPolicyAccepted: boolean;
}

export interface ForgotPasswordRequest {
    email: string;
}

export interface VerifyEmailCodeRequest {
    email: string;
    code: string;
}

export interface ResendEmailVerificationCodeRequest {
    email: string;
}

export interface VerifyPasswordResetCodeRequest {
    email: string;
    code: string;
}

export interface ResendPasswordResetCodeRequest {
    email: string;
}

export interface ConfirmEmailTokenRequest {
    userId: string;
    token: string;
}

export interface ResetPasswordRequest {
    email: string;
    code: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface AuthResponse {
    success: boolean;
    message: string;
    data?: TokenResponse;
    errors?: string[];
}

export interface TokenResponse {
    accessToken: string;
    refreshToken: string;
    expiryDate: string;
}

export interface UserInfo {
    id: string;
    firstName: string;
    lastName: string;
    fullName: string;
    email: string;
    emailConfirmed: boolean;
    profilePicture?: string;
    roles: string[];
    activeRole?: string | null;
    hasPassword: boolean;
}

export interface MfaStatus {
    featureEnabled: boolean;
    enabled: boolean;
}

export interface UpdateMfaRequest {
    enabled: boolean;
    currentPassword?: string;
    code?: string;
}

export interface SendMfaCodeRequest {
    targetState: boolean;
}

export interface UpdateUserInfoRequest {
    firstName: string;
    lastName: string;
    email: string;
    profilePicture?: string;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface GenericAuthResponse<T> {
    success: boolean;
    message: string;
    data?: T;
    errors?: string[];
}

export interface SwitchRoleRequest {
    roleName: string;
}

export interface RoleSwitchResult {
    activeRole: string;
    roles: string[];
}

export interface UserSession {
    id: string;
    createdAtUtc: string;
    lastUsedAtUtc?: string;
    expiresAtUtc: string;
    isCurrent: boolean;
    isRevoked: boolean;
    isExpired: boolean;
    ipAddress?: string;
    userAgent?: string;
}
