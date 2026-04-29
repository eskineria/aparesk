import axios from 'axios';
import i18n from '@/i18n';
import { showToast, showValidationToast } from '@/utils/toast';

const AUTH_SESSION_MARKER_KEY = 'eskineria.authenticated-session';

const api = axios.create({
    baseURL: `${import.meta.env.VITE_API_BASE_URL}/api/${import.meta.env.VITE_API_VERSION}`,
    headers: {
        'Content-Type': 'application/json',
    },
    withCredentials: true, // Important for cookies/sessions if used
    timeout: 120000, // 2 minutes timeout
});

// Request interceptor to add language header
api.interceptors.request.use(
    (config) => {
        // Add language header
        config.headers['Accept-Language'] = i18n.language;

        const workspaceId = localStorage.getItem('workspaceId')?.trim();
        if (workspaceId) {
            config.headers['X-Workspace-Id'] = workspaceId;
        } else {
            delete config.headers['X-Workspace-Id'];
        }

        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response interceptor to handle global errors
api.interceptors.response.use(
    (response) => response,
    (error) => {
        // Handle cancelled requests gracefully
        if (axios.isCancel(error) || error.code === 'ERR_CANCELED') {
            return Promise.reject(error);
        }

        const { response } = error;

        if (response) {
            const { status, data } = response;
            // Support for standardized ProblemDetails (RFC 7807) used by Eskineria.ExceptionHandler
            const title = data.title || i18n.t('common.error');
            const detail = data.detail || data.message || '';
            const errors = data.errors;
            const errorCodes = Array.isArray(errors) ? errors : [];

            if (status === 400) {
                // Suppress toasts on reset-password page (errors shown in UI)
                const isOnResetPassword = window.location.pathname.includes('/reset-password');

                // Check if errors exist and are not empty
                const hasErrors = errors && (
                    (Array.isArray(errors) && errors.length > 0) ||
                    (typeof errors === 'object' && Object.keys(errors).length > 0)
                );

                if (hasErrors && !isOnResetPassword) {
                    showValidationToast(errors);
                } else if (!isOnResetPassword) {
                    // Start: Suppress "Already Confirmed" toast
                    const msg = (detail || title || '').toLowerCase();
                    const isAlreadyConfirmed =
                        msg.includes('already confirmed') ||
                        msg.includes('zaten onaylanmış') ||
                        msg.includes('zaten onaylı') ||
                        msg.includes('already verified');
                    const requestUrl = (error.config?.url || '').toLowerCase();
                    const isRegisterRequest = requestUrl.includes('/auth/register');
                    const isRegistrationDisabled =
                        isRegisterRequest && (
                            msg.includes('registrations are currently disabled') ||
                            msg.includes('registration is currently disabled') ||
                            msg.includes('registration is disabled') ||
                            msg.includes('yeni kullanıcı kaydı şu anda kapalı') ||
                            msg.includes('kullanıcı kaydı şu anda kapalı') ||
                            msg.includes('kayıt şu anda kapalı')
                        );

                    if (!isAlreadyConfirmed && !isRegistrationDisabled) {
                        showToast(detail || title, 'error');
                    }
                    // End: Suppress
                }
            } else if (status === 401) {
                // Don't show toast or redirect if on reset-password page
                const isOnResetPassword = window.location.pathname.includes('/reset-password');
                const isOnLogin = window.location.pathname.includes('/auth/login');
                const otherPublicPaths = ['/auth/register', '/auth/forgot-password', '/auth/captcha', '/confirm-email'];
                const isOnOtherPublicPage = otherPublicPaths.some(path => window.location.pathname.includes(path));

                if (isOnLogin) {
                    // On login page, show toast for invalid credentials but don't redirect
                    const isEmailNotConfirmed = errorCodes.includes('EMAIL_NOT_CONFIRMED');
                    if (!isEmailNotConfirmed) {
                        showToast(
                            detail || title || i18n.t('auth.errors.invalidCredentials') || i18n.t('auth.errors.unauthorized'),
                            'error'
                        );
                    }
                } else if (!isOnOtherPublicPage && !isOnResetPassword) {
                    // On protected pages, only show expiry warning if the app had an authenticated session.
                    const hadAuthenticatedSession = sessionStorage.getItem(AUTH_SESSION_MARKER_KEY) === '1';
                    if (hadAuthenticatedSession) {
                        showToast(i18n.t('auth.errors.sessionExpired') || i18n.t('auth.errors.unauthorized'), 'warning');
                    }
                    sessionStorage.removeItem(AUTH_SESSION_MARKER_KEY);
                    window.location.href = '/auth/login';
                }
                // If on other public pages or reset-password, just let the error propagate without toast/redirect
            } else if (status === 403) {
                showToast(i18n.t('auth.errors.unauthorized') || 'Unauthorized Access', 'error');
                // Removed global redirect to prevent background call failures from killing the UI session
            } else if (status === 404) {
                // Usually silent or specific handling in component
                showToast(detail || title || i18n.t('auth.captcha.rateLimitExceeded'), 'warning');
            } else if (status === 500) {
                // Show toast instead of redirecting to error page to prevent session loss
                showToast(detail || title || i18n.t('common.error'), 'error');
            } else if (status === 503) {
                // Maintenance mode - only redirect if not already on the maintenance page
                const isAuthPage = window.location.pathname.startsWith('/auth/');
                if (!window.location.pathname.includes('/maintenance') && !isAuthPage) {
                    window.location.href = '/maintenance';
                }
            } else {
                showToast(detail || title || i18n.t('common.error'), 'error');
            }
        } else if (error.request) {
            // Network error or timeout
            if (error.code === 'ECONNABORTED') {
                showToast(i18n.t('auth.errors.requestTimeout') || i18n.t('common.networkError'), 'error');
            } else {
                // Show toast for network issues instead of redirecting
                showToast(i18n.t('common.networkError') || 'Network Error', 'error');
            }
        } else {
            showToast(error.message, 'error');
        }

        return Promise.reject(error);
    }
);

export default api;
