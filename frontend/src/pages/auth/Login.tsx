import { Link, useNavigate } from 'react-router-dom'
import { FormControl, FormLabel, Button, Alert } from 'react-bootstrap'
import { LuLock, LuMail, LuLoader } from 'react-icons/lu'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { useState, useEffect, useRef, useCallback } from 'react'
import { AuthService } from '@/services/authService'
import SystemSettingsService from '@/services/systemSettingsService'
import { useAuth } from '@/context/AuthContext'
import type { LoginRequest, AuthResponse, SocialLoginRequest } from '@/types/auth'
import type { AuthSystemSettings } from '@/types/systemSettings'
import AuthShell from './components/AuthShell'

type GoogleCredentialResponse = {
  credential?: string;
};

type GoogleAccountsId = {
  initialize: (config: {
    client_id: string;
    callback: (response: GoogleCredentialResponse) => void;
  }) => void;
  renderButton: (
    parent: HTMLElement,
    options: {
      type?: 'standard' | 'icon';
      theme?: 'outline' | 'filled_blue' | 'filled_black';
      size?: 'large' | 'medium' | 'small';
      text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
      shape?: 'rectangular' | 'pill' | 'circle' | 'square';
      logo_alignment?: 'left' | 'center';
      width?: number;
    }
  ) => void;
};

declare global {
  interface Window {
    google?: {
      accounts?: {
        id?: GoogleAccountsId;
      };
    };
  }
}

const DEFAULT_AUTH_SETTINGS: AuthSystemSettings = {
  loginEnabled: true,
  registerEnabled: true,
  googleLoginEnabled: true,
  forgotPasswordEnabled: true,
  changePasswordEnabled: true,
  sessionManagementEnabled: true,
  emailVerificationRequired: true,
  emailVerificationCodeExpirySeconds: 180,
  emailVerificationResendCooldownSeconds: 60,
  maintenanceModeEnabled: false,
}

const buildConfirmEmailStorageKey = () => 'confirm-email-address'
const buildConfirmExpiryStorageKey = (email: string) =>
  `confirm-email-expiry:${email.trim().toLowerCase()}`
const buildConfirmCooldownStorageKey = (email: string) =>
  `confirm-email-cooldown:${email.trim().toLowerCase()}`

const Index = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { refresh } = useAuth();
  const { register, handleSubmit, setValue, formState: { isSubmitting } } = useForm<LoginRequest>();
  const emptyMfaDigits = () => Array.from({ length: 6 }, () => '');
  const [rememberMe, setRememberMe] = useState(false);
  const [isMfaStep, setIsMfaStep] = useState(false);
  const [mfaDigits, setMfaDigits] = useState<string[]>(emptyMfaDigits);
  const [isGoogleLoading, setIsGoogleLoading] = useState(false);
  const [loginType, setLoginType] = useState<'credentials' | 'social'>('credentials');
  const [socialPayload, setSocialPayload] = useState<SocialLoginRequest | null>(null);
  const [authSettings, setAuthSettings] = useState<AuthSystemSettings>(DEFAULT_AUTH_SETTINGS);
  const googleButtonRef = useRef<HTMLDivElement | null>(null);
  const mfaInputRefs = useRef<Array<HTMLInputElement | null>>([]);
  const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined;
  const isLoginEnabled = authSettings.loginEnabled;
  const isGoogleEnabled =
    isLoginEnabled &&
    authSettings.googleLoginEnabled &&
    Boolean(googleClientId && googleClientId.trim().length > 0);
  const isForgotPasswordEnabled = authSettings.forgotPasswordEnabled;
  const isRegisterEnabled = authSettings.registerEnabled;
  const isMaintenanceModeEnabled = authSettings.maintenanceModeEnabled;
  const isMfaCodeComplete = mfaDigits.every((digit) => digit.length === 1);

  useEffect(() => {
    const savedEmail = localStorage.getItem('rememberedEmail');
    if (savedEmail) {
      setValue('email', savedEmail);
      setRememberMe(true);
    }
  }, [setValue]);

  useEffect(() => {
    const loadAuthSettings = async () => {
      try {
        const response = await SystemSettingsService.getPublicAuthSettings();
        if (response.success && response.data) {
          setAuthSettings(response.data);
        }
      } catch {
        // Global axios interceptor handles errors.
      }
    };

    void loadAuthSettings();
  }, []);

  useEffect(() => {
    setValue('mfaCode', mfaDigits.join(''));
  }, [mfaDigits, setValue]);

  useEffect(() => {
    if (!isMfaStep) {
      return;
    }

    const timerId = window.setTimeout(() => {
      const firstEmptyIndex = mfaDigits.findIndex((digit) => digit.length === 0);
      const targetIndex = firstEmptyIndex >= 0 ? firstEmptyIndex : mfaDigits.length - 1;
      mfaInputRefs.current[targetIndex]?.focus();
    }, 0);

    return () => window.clearTimeout(timerId);
  }, [isMfaStep, mfaDigits]);

  const handleLoginSuccess = useCallback(async () => {
    const authenticated = await refresh(true);
    if (!authenticated) {
      return;
    }
    sessionStorage.setItem('loginSuccessToast', t('auth.login.loginSuccess'));
    navigate('/');
  }, [navigate, refresh, t]);

  const handleGoogleCredential = useCallback(async (googleResponse: GoogleCredentialResponse) => {
    const credential = googleResponse.credential?.trim();
    if (!credential) {
      return;
    }

    setIsGoogleLoading(true);
    try {
      const payload: SocialLoginRequest = {
        provider: 'google',
        idToken: credential
      };

      setLoginType('social');
      setSocialPayload(payload);

      const response = await AuthService.socialLogin(payload);
      if (response.success) {
        await handleLoginSuccess();
      }
    } catch (err: any) {
      const responseData = err?.response?.data as AuthResponse | undefined;
      const errorCodes = Array.isArray(responseData?.errors) ? responseData.errors : [];

      if (errorCodes.includes('MFA_REQUIRED') || errorCodes.includes('MFA_INVALID')) {
        setIsMfaStep(true);
        setMfaDigits(emptyMfaDigits());
        setValue('mfaCode', '');
      }
    } finally {
      setIsGoogleLoading(false);
    }
  }, [handleLoginSuccess, setValue]);

  useEffect(() => {
    if (!isGoogleEnabled || !googleButtonRef.current) {
      return;
    }

    const scriptId = 'google-identity-services-client';
    let isDisposed = false;

    const initializeGoogleButton = () => {
      if (isDisposed || !googleButtonRef.current) {
        return;
      }

      const googleId = window.google?.accounts?.id;
      if (!googleId) {
        return;
      }

      googleId.initialize({
        client_id: googleClientId!,
        callback: (credentialResponse) => {
          void handleGoogleCredential(credentialResponse);
        }
      });

      const buttonWidth = Math.max(220, Math.min(360, googleButtonRef.current.clientWidth || 320));
      googleButtonRef.current.innerHTML = '';
      googleId.renderButton(googleButtonRef.current, {
        theme: 'outline',
        size: 'large',
        text: 'continue_with',
        shape: 'pill',
        width: buttonWidth
      });
    };

    let script = document.getElementById(scriptId) as HTMLScriptElement | null;
    if (!script) {
      script = document.createElement('script');
      script.id = scriptId;
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      document.head.appendChild(script);
    }

    script.addEventListener('load', initializeGoogleButton);

    if (window.google?.accounts?.id) {
      initializeGoogleButton();
    }

    return () => {
      isDisposed = true;
      script?.removeEventListener('load', initializeGoogleButton);
    };
  }, [googleClientId, handleGoogleCredential, isGoogleEnabled]);

  const handleMfaCodeChange = (index: number, value: string) => {
    const nextDigit = value.replace(/\D/g, '').slice(-1);

    setMfaDigits((prev) => {
      const next = [...prev];
      next[index] = nextDigit;
      return next;
    });

    if (nextDigit && index < 5) {
      mfaInputRefs.current[index + 1]?.focus();
    }
  };

  const handleMfaCodeKeyDown = (index: number, event: React.KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    if (event.key === 'Backspace') {
      if (mfaDigits[index]) {
        setMfaDigits((prev) => {
          const next = [...prev];
          next[index] = '';
          return next;
        });
        return;
      }

      if (index > 0) {
        setMfaDigits((prev) => {
          const next = [...prev];
          next[index - 1] = '';
          return next;
        });
        mfaInputRefs.current[index - 1]?.focus();
      }
    }

    if (event.key === 'ArrowLeft' && index > 0) {
      mfaInputRefs.current[index - 1]?.focus();
    }

    if (event.key === 'ArrowRight' && index < 5) {
      mfaInputRefs.current[index + 1]?.focus();
    }
  };

  const handleMfaCodePaste = (event: React.ClipboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const pastedDigits = event.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
    if (!pastedDigits) {
      return;
    }

    event.preventDefault();

    const next = emptyMfaDigits();
    pastedDigits.split('').forEach((digit, index) => {
      next[index] = digit;
    });

    setMfaDigits(next);

    const focusIndex = Math.min(pastedDigits.length, 6) - 1;
    if (focusIndex >= 0) {
      mfaInputRefs.current[focusIndex]?.focus();
    }
  };

  const handleMfaCodeResend = () => {
    setMfaDigits(emptyMfaDigits());
    setValue('mfaCode', '');

    if (loginType === 'social' && socialPayload) {
      void handleGoogleCredential({ credential: socialPayload.idToken });
    } else {
      void handleSubmit(onSubmit)();
    }
  };

  const onSubmit = async (data: LoginRequest) => {
    if (!isLoginEnabled) {
      return;
    }

    if (!isMfaStep) {
      setLoginType('credentials');
      setSocialPayload(null);
    }

    try {
      if (rememberMe && data.email) {
        localStorage.setItem('rememberedEmail', data.email);
      } else {
        localStorage.removeItem('rememberedEmail');
      }

      if (isMfaStep && loginType === 'social' && socialPayload) {
        const response = await AuthService.socialLogin({
          ...socialPayload,
          mfaCode: data.mfaCode
        });
        if (response.success) {
          setIsMfaStep(false);
          setMfaDigits(emptyMfaDigits());
          setValue('mfaCode', '');
          await handleLoginSuccess();
        }
        return;
      }

      const response = await AuthService.login(data);
      if (response.success) {
        setIsMfaStep(false);
        setMfaDigits(emptyMfaDigits());
        setValue('mfaCode', '');
        await handleLoginSuccess();
      }
    } catch (err: any) {
      const responseData = err?.response?.data as AuthResponse | undefined;
      const errorCodes = Array.isArray(responseData?.errors) ? responseData.errors : [];

      if (errorCodes.includes('EMAIL_NOT_CONFIRMED') && data.email) {
        const normalizedEmail = data.email.trim()
        sessionStorage.setItem(buildConfirmEmailStorageKey(), normalizedEmail)

        const message = responseData?.message?.toLowerCase() ?? ''
        const sentFreshCode =
          message.includes('new verification code') ||
          message.includes('yeni bir doğrulama kodu')

        if (sentFreshCode) {
          const expiresAt = Date.now() + authSettings.emailVerificationCodeExpirySeconds * 1000
          const cooldownUntil = Date.now() + authSettings.emailVerificationResendCooldownSeconds * 1000
          sessionStorage.setItem(buildConfirmExpiryStorageKey(normalizedEmail), expiresAt.toString())
          sessionStorage.setItem(buildConfirmCooldownStorageKey(normalizedEmail), cooldownUntil.toString())
        }

        navigate('/confirm-email?from=login')
        return;
      }

      if (errorCodes.includes('MFA_REQUIRED') || errorCodes.includes('MFA_INVALID')) {
        setIsMfaStep(true);
        setMfaDigits(emptyMfaDigits());
        setValue('mfaCode', '');
      }
    }
  };

  return (
    <AuthShell
      footer={(
        <p className="text-muted text-center mt-4 mb-0">
          {isRegisterEnabled && (
            <>
              {t('auth.login.newHere')}{' '}
              <Link to="/auth/register" className="text-decoration-underline link-offset-3 fw-semibold">
                {t('auth.login.createAccount')}
              </Link>
            </>
          )}
        </p>
      )}>
      <h3 className="text-center">{t('auth.login.title')}</h3>
      <p className="text-muted text-center auth-sub-text mx-auto">{t('auth.login.subtitle')}</p>
                {isMaintenanceModeEnabled && (
                  <Alert variant="warning" className="mt-3">
                    {t('auth.login.maintenanceModeEnabled')}
                  </Alert>
                )}
                {!isLoginEnabled && (
                  <Alert variant="warning" className="mt-3">
                    {t('auth.login.loginDisabled')}
                  </Alert>
                )}

                <form className="mt-4" onSubmit={handleSubmit(onSubmit)}>
                  <input type="hidden" {...register('mfaCode')} />
                  <div className="mb-3">
                    <FormLabel htmlFor="userEmail">
                      {t('auth.login.emailLabel')} <span className="text-danger">*</span>
                    </FormLabel>
                    <div className="app-search">
                      <FormControl
                        type="email"
                        id="userEmail"
                        placeholder={t('auth.login.emailPlaceholder')}
                        disabled={!isLoginEnabled}
                        {...register('email')}
                      />
                      <LuMail className="app-search-icon text-muted" />
                    </div>
                  </div>
                  <div className="mb-3">
                    <FormLabel htmlFor="userPassword">
                      {t('auth.login.passwordLabel')} <span className="text-danger">*</span>
                    </FormLabel>
                    <div className="app-search">
                      <FormControl
                        type="password"
                        id="userPassword"
                        placeholder={t('auth.login.passwordPlaceholder')}
                        disabled={!isLoginEnabled}
                        {...register('password')}
                      />
                      <LuLock className="app-search-icon text-muted" />
                    </div>
                  </div>
                  {isMfaStep && (
                    <>
                      <Alert variant="info" className="py-2">
                        {t('auth.login.mfaHint')}
                      </Alert>
                      <div className="mb-3">
                        <FormLabel htmlFor="mfaCode0">
                          {t('auth.login.mfaCodeLabel')} <span className="text-danger">*</span>
                        </FormLabel>
                        <div className="d-flex justify-content-center gap-2">
                          {mfaDigits.map((digit, index) => (
                            <FormControl
                              key={`mfa-code-${index}`}
                              id={`mfaCode${index}`}
                              type="text"
                              inputMode="numeric"
                              maxLength={1}
                              autoComplete={index === 0 ? 'one-time-code' : 'off'}
                              value={digit}
                              onChange={(event) => handleMfaCodeChange(index, event.target.value)}
                              onKeyDown={(event) => handleMfaCodeKeyDown(index, event)}
                              onPaste={handleMfaCodePaste}
                              onFocus={(event) => event.currentTarget.select()}
                              disabled={!isLoginEnabled}
                              ref={(input) => {
                                mfaInputRefs.current[index] = input;
                              }}
                              className="text-center fw-semibold"
                              style={{ width: 46, fontSize: '1.2rem' }}
                            />
                          ))}
                        </div>
                        <div className="d-flex justify-content-end mt-2">
                          <Button
                            type="button"
                            variant="link"
                            className="px-0"
                            disabled={isSubmitting || !isLoginEnabled}
                            onClick={handleMfaCodeResend}>
                            {t('auth.login.resendMfaCodeButton')}
                          </Button>
                        </div>
                      </div>
                    </>
                  )}
                  <div className="d-flex justify-content-between align-items-center mb-3">
                    <div className="form-check">
                      <input
                        className="form-check-input form-check-input-light fs-14"
                        type="checkbox"
                        id="rememberMe"
                        checked={rememberMe}
                        disabled={!isLoginEnabled}
                        onChange={(e) => setRememberMe(e.target.checked)}
                      />
                      <label className="form-check-label" htmlFor="rememberMe">
                        {t('auth.login.rememberMe')}
                      </label>
                    </div>
                    {isForgotPasswordEnabled ? (
                      <Link to="/auth/forgot-password" className="text-decoration-underline link-offset-3 text-muted">
                        {t('auth.login.forgotPassword')}
                      </Link>
                    ) : (
                      <span className="text-muted small">{t('auth.login.forgotPasswordDisabled')}</span>
                    )}
                  </div>
                  <div className="d-grid">
                    <Button
                      type="submit"
                      variant="primary"
                      className="fw-semibold py-2"
                      disabled={isSubmitting || !isLoginEnabled || (isMfaStep && !isMfaCodeComplete)}>
                      {isSubmitting ? <LuLoader className="icon-spin me-1" /> : null}
                      {isSubmitting
                        ? t('common.loading')
                        : (isMfaStep ? t('auth.login.verifyMfaButton') : t('auth.login.submitButton'))}
                    </Button>
                  </div>
                  {isGoogleEnabled && (
                    <>
                      <div className="position-relative my-3">
                        <hr className="my-0" />
                        <span className="position-absolute top-50 start-50 translate-middle bg-body px-2 text-muted small">
                          {t('auth.login.orContinueWith')}
                        </span>
                      </div>
                      <div className="d-flex justify-content-center">
                        <div
                          ref={googleButtonRef}
                          className={isGoogleLoading ? 'opacity-50 pe-none' : ''}
                          style={{ minHeight: 44 }}
                        />
                      </div>
                    </>
                  )}
                </form>
    </AuthShell>
  )
}

export default Index
