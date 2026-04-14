import api from '@/api/axios';
import type {
    LoginRequest,
    SocialLoginRequest,
    RegisterRequest,
    AuthResponse,
    ForgotPasswordRequest,
    VerifyEmailCodeRequest,
    ResendEmailVerificationCodeRequest,
    VerifyPasswordResetCodeRequest,
    ResendPasswordResetCodeRequest,
    ResetPasswordRequest,
    UserInfo,
    UserSession,
    SwitchRoleRequest,
    RoleSwitchResult,
    GenericAuthResponse,
    UpdateUserInfoRequest,
    ChangePasswordRequest,
    MfaStatus,
    UpdateMfaRequest,
    SendMfaCodeRequest
} from '@/types/auth';

export const AuthService = {
    login: async (data: LoginRequest) => {
        const response = await api.post<AuthResponse>('/auth/login', data);
        return response.data;
    },
    socialLogin: async (data: SocialLoginRequest) => {
        const response = await api.post<AuthResponse>('/auth/social-login', data);
        return response.data;
    },
    register: async (data: RegisterRequest) => {
        const response = await api.post<AuthResponse>('/auth/register', data);
        return response.data;
    },
    forgotPassword: async (data: ForgotPasswordRequest) => {
        const response = await api.post<AuthResponse>('/auth/forgot-password', data);
        return response.data;
    },
    confirmEmail: async (data: VerifyEmailCodeRequest) => {
        const response = await api.post<AuthResponse>('/auth/confirm-email', data);
        return response.data;
    },
    resendConfirmationCode: async (data: ResendEmailVerificationCodeRequest) => {
        const response = await api.post<AuthResponse>('/auth/resend-confirmation-code', data);
        return response.data;
    },
    verifyResetPasswordCode: async (data: VerifyPasswordResetCodeRequest) => {
        const response = await api.post<AuthResponse>('/auth/verify-reset-password-code', data);
        return response.data;
    },
    resendResetPasswordCode: async (data: ResendPasswordResetCodeRequest) => {
        const response = await api.post<AuthResponse>('/auth/resend-reset-password-code', data);
        return response.data;
    },
    resetPassword: async (data: ResetPasswordRequest) => {
        const response = await api.post<AuthResponse>('/auth/reset-password', data);
        return response.data;
    },
    logout: async () => {
        const response = await api.post('/auth/logout');
        return response.data;
    },
    getUserInfo: async () => {
        const response = await api.get<GenericAuthResponse<UserInfo>>('/auth/user-info');
        return response.data;
    },
    getMfaStatus: async () => {
        const response = await api.get<GenericAuthResponse<MfaStatus>>('/auth/mfa-status');
        return response.data;
    },
    sendMfaCode: async (data: SendMfaCodeRequest) => {
        const response = await api.post<AuthResponse>('/auth/send-mfa-code', data);
        return response.data;
    },
    updateMfa: async (data: UpdateMfaRequest) => {
        const response = await api.post<GenericAuthResponse<MfaStatus>>('/auth/mfa', data);
        return response.data;
    },
    getPermissions: async () => {
        const response = await api.get<GenericAuthResponse<string[]>>('/auth/permissions');
        return response.data;
    },
    getSessions: async () => {
        const response = await api.get<GenericAuthResponse<UserSession[]>>('/auth/sessions');
        return response.data;
    },
    revokeSession: async (sessionId: string) => {
        const response = await api.delete<AuthResponse>(`/auth/sessions/${sessionId}`);
        return response.data;
    },
    revokeOtherSessions: async () => {
        const response = await api.post<AuthResponse>('/auth/sessions/revoke-others');
        return response.data;
    },
    switchRole: async (data: SwitchRoleRequest) => {
        const response = await api.post<GenericAuthResponse<RoleSwitchResult>>('/auth/switch-role', data);
        return response.data;
    },
    updateUserInfo: async (data: UpdateUserInfoRequest) => {
        const response = await api.put<AuthResponse>('/auth/update-info', data);
        return response.data;
    },
    changePassword: async (data: ChangePasswordRequest) => {
        const response = await api.post<AuthResponse>('/auth/change-password', data);
        return response.data;
    },
    refreshToken: async () => {
        const response = await api.post<AuthResponse>('/auth/refresh-token');
        return response.data;
    }
};
