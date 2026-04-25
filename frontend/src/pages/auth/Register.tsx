import DOMPurify from 'dompurify'
import AppLogo from '@/components/AppLogo'
import LanguageDropdown from '@/components/LanguageDropdown'
import PasswordInputWithStrength from '@/components/PasswordInputWithStrength'
import { author, currentYear } from '@/helpers'
import { Link, useNavigate } from 'react-router-dom'
import { useEffect, useRef, useState } from 'react'
import { Card, CardBody, Col, FormControl, FormLabel, Row, Modal, Button, Alert } from 'react-bootstrap'
import { LuCircleUser, LuMail, LuLock } from 'react-icons/lu'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { AuthService } from '@/services/authService'
import { ComplianceService } from '@/services/complianceService'
import SystemSettingsService from '@/services/systemSettingsService'
import { useBranding } from '@/context/BrandingContext'
import type { RegisterRequest } from '@/types/auth'
import type { TermsDto } from '@/types/compliance'
import type { AuthSystemSettings } from '@/types/systemSettings'
import { showToast } from '@/utils/toast'

type RegisterTermsType = 'TermsOfService' | 'PrivacyPolicy'

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

const Index = () => {
  const emptyCodeDigits = () => Array.from({ length: 6 }, () => '');
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { applicationName } = useBranding();
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showTermsModal, setShowTermsModal] = useState(false);
  const [isRegistered, setIsRegistered] = useState(false);
  const [isEmailVerified, setIsEmailVerified] = useState(false);
  const [registeredEmail, setRegisteredEmail] = useState('');
  const [verificationCodeDigits, setVerificationCodeDigits] = useState<string[]>(emptyCodeDigits);
  const verificationCodeInputRefs = useRef<Array<HTMLInputElement | null>>([]);
  const lastAutoSubmitRef = useRef('');
  const [verificationExpiresIn, setVerificationExpiresIn] = useState(180);
  const [resendCooldownSeconds, setResendCooldownSeconds] = useState(0);
  const [verificationMessage, setVerificationMessage] = useState('');
  const [verificationError, setVerificationError] = useState('');
  const [isVerifyingCode, setIsVerifyingCode] = useState(false);
  const [isResendingCode, setIsResendingCode] = useState(false);
  const [authSettings, setAuthSettings] = useState<AuthSystemSettings>(DEFAULT_AUTH_SETTINGS);
  const [isSettingsLoading, setIsSettingsLoading] = useState(true);
  const [termsDocuments, setTermsDocuments] = useState<Partial<Record<RegisterTermsType, TermsDto | null>>>({});
  const [activeTermsType, setActiveTermsType] = useState<RegisterTermsType | null>(null);
  const [isLoadingTerms, setIsLoadingTerms] = useState(false);
  const { register, handleSubmit, setValue, formState: { isSubmitting } } = useForm<RegisterRequest>();

  const fetchActiveTerms = async (type: RegisterTermsType) => {
    setIsLoadingTerms(true);
    try {
      const response = await ComplianceService.getActiveTermsOptional(type);
      setTermsDocuments((prev) => ({
        ...prev,
        [type]: response.success ? response.data : null,
      }));
    } catch (err) {
      console.error('Failed to fetch terms:', err);
    } finally {
      setIsLoadingTerms(false);
    }
  };

  useEffect(() => {
    const loadRequiredDocuments = async () => {
      await Promise.all([
        fetchActiveTerms('TermsOfService'),
        fetchActiveTerms('PrivacyPolicy'),
      ]);
    };

    void loadRequiredDocuments();
  }, []);

  const openTermsModal = (type: RegisterTermsType) => {
    setActiveTermsType(type);
    setShowTermsModal(true);

    if (termsDocuments[type] === undefined) {
      void fetchActiveTerms(type);
    }
  };

  const activeTerms = activeTermsType ? (termsDocuments[activeTermsType] ?? null) : null;
  const activeTermsModalTitle = activeTermsType === 'PrivacyPolicy'
    ? t('auth.register.privacyPolicyModalTitle')
    : t('auth.register.termsOfServiceModalTitle');

  useEffect(() => {
    const loadAuthSettings = async () => {
      setIsSettingsLoading(true);
      try {
        const response = await SystemSettingsService.getPublicAuthSettings();
        if (response.success && response.data) {
          setAuthSettings(response.data);
        }
      } catch {
        // Global axios interceptor handles errors.
      } finally {
        setIsSettingsLoading(false);
      }
    };

    void loadAuthSettings();
  }, []);

  useEffect(() => {
    if (!isRegistered || isEmailVerified || verificationExpiresIn <= 0) {
      return;
    }

    const timer = window.setInterval(() => {
      setVerificationExpiresIn((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);

    return () => window.clearInterval(timer);
  }, [isRegistered, isEmailVerified, verificationExpiresIn]);

  useEffect(() => {
    if (!isRegistered || isEmailVerified || resendCooldownSeconds <= 0) {
      return;
    }

    const timer = window.setInterval(() => {
      setResendCooldownSeconds((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);

    return () => window.clearInterval(timer);
  }, [isRegistered, isEmailVerified, resendCooldownSeconds]);

  useEffect(() => {
    if (!isRegistered || isEmailVerified || verificationExpiresIn > 0) {
      return;
    }

    setVerificationCodeDigits(emptyCodeDigits());
    setVerificationError(t('auth.register.codeExpired'));
    lastAutoSubmitRef.current = '';
  }, [isRegistered, isEmailVerified, verificationExpiresIn, t]);

  const isRegisterBlockedByMaintenance = authSettings.maintenanceModeEnabled;

  useEffect(() => {
    if (!isSettingsLoading && (!authSettings.registerEnabled || isRegisterBlockedByMaintenance)) {
      navigate('/auth/login');
    }
  }, [isSettingsLoading, authSettings.registerEnabled, isRegisterBlockedByMaintenance, navigate]);

  const formatCountdown = (seconds: number) => {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
  };

  const onSubmit = async (data: RegisterRequest) => {
    if (!authSettings.registerEnabled) {
      return;
    }

    // Manually add password explicitly if needed or rely on register
    data.password = password;
    data.confirmPassword = confirmPassword;

    try {
      const response = await AuthService.register(data);
      if (response.success) {
        if (response.data) {
          showToast(response.message || t('auth.register.registrationCompleted'), 'success');
          navigate('/');
          return;
        }

        setRegisteredEmail(data.email ?? '');
        setIsRegistered(true);
        setIsEmailVerified(false);
        setVerificationCodeDigits(emptyCodeDigits());
        lastAutoSubmitRef.current = '';
        setVerificationExpiresIn(authSettings.emailVerificationCodeExpirySeconds);
        setResendCooldownSeconds(authSettings.emailVerificationResendCooldownSeconds);
        setVerificationMessage('');
        setVerificationError('');
      }
    } catch (err: any) {
      // Handled by global interceptor
    }
  };

  const handleVerificationCodeChange = (index: number, value: string) => {
    const currentDigit = verificationCodeDigits[index]
    const newVal = value.replace(/\D/g, '')

    let nextDigit = ''
    if (newVal.length > 1 && currentDigit) {
      if (newVal[0] === currentDigit) {
        nextDigit = newVal[1]
      } else {
        nextDigit = newVal[0]
      }
    } else {
      nextDigit = newVal.slice(-1)
    }

    setVerificationCodeDigits((prev) => {
      const next = [...prev];
      next[index] = nextDigit;
      return next;
    });

    if (nextDigit && index < 5) {
      verificationCodeInputRefs.current[index + 1]?.focus();
    }
  };

  const handleVerificationCodeKeyDown = (index: number, event: React.KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    if (event.key === 'Backspace') {
      if (verificationCodeDigits[index]) {
        setVerificationCodeDigits((prev) => {
          const next = [...prev];
          next[index] = '';
          return next;
        });
        return;
      }

      if (index > 0) {
        setVerificationCodeDigits((prev) => {
          const next = [...prev];
          next[index - 1] = '';
          return next;
        });
        verificationCodeInputRefs.current[index - 1]?.focus();
      }
    }

    if (event.key === 'ArrowLeft' && index > 0) {
      verificationCodeInputRefs.current[index - 1]?.focus();
    }

    if (event.key === 'ArrowRight' && index < 5) {
      verificationCodeInputRefs.current[index + 1]?.focus();
    }
  };

  const handleVerificationCodePaste = (event: React.ClipboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    if (verificationExpiresIn <= 0 || isVerifyingCode || isResendingCode) {
      return;
    }

    const pastedDigits = event.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
    if (!pastedDigits) {
      return;
    }

    event.preventDefault();

    const next = emptyCodeDigits();
    pastedDigits.split('').forEach((digit, index) => {
      next[index] = digit;
    });

    setVerificationCodeDigits(next);

    const focusIndex = Math.min(pastedDigits.length, 6) - 1;
    if (focusIndex >= 0) {
      verificationCodeInputRefs.current[focusIndex]?.focus();
    }
  };

  const isExpiredMessage = (message: string) => {
    const normalized = message.toLowerCase();
    return normalized.includes('expired') || normalized.includes('süresi doldu');
  };

  const verifyCode = async (verificationCode: string) => {
    setVerificationError('');
    setVerificationMessage('');
    setIsVerifyingCode(true);

    try {
      const response = await AuthService.confirmEmail({
        email: registeredEmail,
        code: verificationCode
      });

      if (response.success) {
        setIsEmailVerified(true);
        return;
      }

      if (isExpiredMessage(response.message)) {
        setVerificationExpiresIn(0);
        setVerificationCodeDigits(emptyCodeDigits());
        lastAutoSubmitRef.current = '';
      }

      setVerificationError(response.message || t('auth.confirmEmail.errorMessage'));
    } catch (err: any) {
      const message = err?.response?.data?.message || err?.message || t('auth.errors.default');

      if (isExpiredMessage(message)) {
        setVerificationExpiresIn(0);
        setVerificationCodeDigits(emptyCodeDigits());
        lastAutoSubmitRef.current = '';
      }

      setVerificationError(message);
    } finally {
      setIsVerifyingCode(false);
    }
  };

  useEffect(() => {
    const isComplete = verificationCodeDigits.every((digit) => digit.length === 1);
    if (!isComplete) {
      lastAutoSubmitRef.current = '';
      return;
    }

    if (!isRegistered || isEmailVerified || isVerifyingCode || verificationExpiresIn <= 0 || !registeredEmail.trim()) {
      return;
    }

    const verificationCode = verificationCodeDigits.join('');
    const attemptKey = `${registeredEmail.trim().toLowerCase()}|${verificationCode}`;
    if (attemptKey === lastAutoSubmitRef.current) {
      return;
    }

    lastAutoSubmitRef.current = attemptKey;
    void verifyCode(verificationCode);
  }, [
    isRegistered,
    isEmailVerified,
    isVerifyingCode,
    registeredEmail,
    verificationCodeDigits,
    verificationExpiresIn
  ]);

  const handleResendCode = async () => {
    if (!registeredEmail) {
      return;
    }

    setVerificationError('');
    setVerificationMessage('');
    setIsResendingCode(true);

    try {
      const response = await AuthService.resendConfirmationCode({ email: registeredEmail });
      if (response.success) {
        setVerificationCodeDigits(emptyCodeDigits());
        lastAutoSubmitRef.current = '';
        setVerificationExpiresIn(authSettings.emailVerificationCodeExpirySeconds);
        setResendCooldownSeconds(authSettings.emailVerificationResendCooldownSeconds);
        setVerificationMessage(response.message || t('auth.register.codeResent'));
        verificationCodeInputRefs.current[0]?.focus();
        return;
      }

      setVerificationError(response.message || t('auth.confirmEmail.errorMessage'));
    } catch (err: any) {
      const message = err?.response?.data?.message || err?.message || t('auth.errors.default');
      setVerificationError(message);
    } finally {
      setIsResendingCode(false);
    }
  };

  if (isRegistered) {
    if (isEmailVerified) {
      return (
        <div className="auth-box p-0 w-100">
          <Row className="w-100 g-0">
            <Col md={'auto'}>
              <Card className="auth-box-form border-0 mb-0">
                <CardBody className="min-vh-100 d-flex flex-column justify-content-center text-center p-5">
                  <div className="mb-4">
                    <div className="avatar-lg mx-auto">
                      <span className="avatar-title bg-soft-success text-success display-4 rounded-circle">
                        <LuMail />
                      </span>
                    </div>
                  </div>
                  <h3 className="fw-semibold mb-3">{t('auth.confirmEmail.successTitle')}</h3>
                  <p className="text-muted mb-4 fs-16">{t('auth.confirmEmail.successMessage')}</p>
                  <div className="d-grid mt-4">
                    <Link to="/auth/login" className="btn btn-primary fw-semibold py-2">
                      {t('auth.confirmEmail.backToDashboard')}
                    </Link>
                  </div>
                </CardBody>
              </Card>
            </Col>
            <Col>
              <div className="h-100 position-relative card-side-img rounded-0 overflow-hidden">
                <div className="p-4 card-img-overlay auth-overlay d-flex align-items-end justify-content-center"></div>
              </div>
            </Col>
          </Row>
        </div>
      )
    }

    return (
      <div className="auth-box p-0 w-100">
        <Row className="w-100 g-0">
          <Col md={'auto'}>
            <Card className="auth-box-form border-0 mb-0">
              <CardBody className="min-vh-100 d-flex flex-column justify-content-center p-5">
                <div className="mb-4">
                  <div className="avatar-lg mx-auto">
                    <span className="avatar-title bg-soft-success text-success display-4 rounded-circle">
                      <LuMail />
                    </span>
                  </div>
                </div>
                <h3 className="fw-semibold mb-3 text-center">{t('auth.register.successTitle')}</h3>
                <p className="text-muted mb-4 fs-16 text-center">{t('auth.register.successMessage')}</p>

                <p className="text-muted text-center mb-3">{registeredEmail}</p>

                {verificationMessage && <Alert variant="success">{verificationMessage}</Alert>}
                {verificationError && <Alert variant="danger">{verificationError}</Alert>}

                <div>
                  <div className="mb-3">
                    <FormLabel htmlFor="register-verification-code-0">{t('auth.register.verificationCodeLabel')}</FormLabel>
                    <div className="d-flex justify-content-center gap-2">
                      {verificationCodeDigits.map((digit, index) => (
                        <FormControl
                          key={`register-code-${index}`}
                          id={`register-verification-code-${index}`}
                          type="text"
                          inputMode="numeric"
                          maxLength={1}
                          autoComplete={index === 0 ? 'one-time-code' : 'off'}
                          value={digit}
                          onChange={(event) => handleVerificationCodeChange(index, event.target.value)}
                          onKeyDown={(event) => handleVerificationCodeKeyDown(index, event)}
                          onPaste={handleVerificationCodePaste}
                          onFocus={(event) => event.currentTarget.select()}
                          onClick={(event) => event.currentTarget.select()}
                          disabled={verificationExpiresIn <= 0 || isVerifyingCode || isResendingCode}
                          ref={(input) => {
                            verificationCodeInputRefs.current[index] = input;
                          }}
                          className="text-center fw-semibold"
                          style={{ width: 46, fontSize: '1.2rem' }}
                        />
                      ))}
                    </div>
                  </div>

                  <p className="text-muted mb-3 text-center">
                    {verificationExpiresIn > 0
                      ? t('auth.register.codeExpiresIn', { time: formatCountdown(verificationExpiresIn) })
                      : t('auth.register.codeExpired')}
                  </p>

                  {isVerifyingCode && (
                    <p className="text-center text-muted mb-3">{t('auth.register.verifyingCode')}</p>
                  )}

                  <div className="d-grid gap-2 mt-3">
                    <Button
                      type="button"
                      variant="outline-secondary"
                      className="fw-semibold py-2"
                      onClick={handleResendCode}
                      disabled={isResendingCode || isVerifyingCode || resendCooldownSeconds > 0}>
                      {isResendingCode
                        ? t('common.loading')
                        : resendCooldownSeconds > 0
                          ? t('auth.confirmEmail.resendCountdown', { seconds: resendCooldownSeconds })
                          : t('auth.register.resendCodeButton')}
                    </Button>
                  </div>
                </div>

                <div className="d-grid mt-4">
                  <Link to="/auth/login" className="btn btn-light fw-semibold py-2">
                    {t('auth.register.loginLink')}
                  </Link>
                </div>
              </CardBody>
            </Card>
          </Col>
          <Col>
            <div className="h-100 position-relative card-side-img rounded-0 overflow-hidden">
              <div className="p-4 card-img-overlay auth-overlay d-flex align-items-end justify-content-center"></div>
            </div>
          </Col>
        </Row>
      </div>
    )
  }

  if (!isSettingsLoading && (!authSettings.registerEnabled || isRegisterBlockedByMaintenance)) {
    return (
      <div className="auth-box p-0 w-100 min-vh-100 d-flex align-items-center justify-content-center bg-light">
        {/* Empty screen while redirecting */}
      </div>
    );
  }

  return (
    <div className="auth-box p-0 w-100">
      <Row className="w-100 g-0">
        <Col md={'auto'}>
          <Card className="auth-box-form border-0 mb-0">
            <div className="position-absolute top-0 end-0" style={{ width: 180 }}>
              <svg
                style={{ opacity: '0.075', width: '100%', height: 'auto' }}
                width={600}
                height={560}
                viewBox="0 0 600 560"
                fill="none"
                xmlns="http://www.w3.org/2000/svg">
                <g clipPath="url(#clip0_948_1464)">
                  <mask id="mask0_948_1464" style={{ maskType: 'luminance' }} maskUnits="userSpaceOnUse" x={0} y={0} width={600} height={1200}>
                    <path d="M0 0L0 1200H600L600 0H0Z" fill="white" />
                  </mask>
                  <g mask="url(#mask0_948_1464)">
                    <path d="M537.448 166.697L569.994 170.892L550.644 189.578L537.448 166.697Z" fill="#FF4C3E" />
                  </g>
                  <mask id="mask1_948_1464" style={{ maskType: 'luminance' }} maskUnits="userSpaceOnUse" x={0} y={0} width={600} height={1200}>
                    <path d="M0 0L0 1200H600L600 0H0Z" fill="white" />
                  </mask>
                  <g mask="url(#mask1_948_1464)">
                    <path
                      d="M364.093 327.517L332.306 359.304C321.885 369.725 304.989 369.725 294.568 359.304L262.781 327.517C252.36 317.096 252.36 300.2 262.781 289.779L294.568 257.992C304.989 247.571 321.885 247.571 332.306 257.992L364.093 289.779C374.514 300.2 374.514 317.096 364.093 327.517Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M377.923 101.019L315.106 163.836C299.517 179.425 274.242 179.425 258.653 163.836L195.836 101.019C180.247 85.4301 180.247 60.1551 195.836 44.5661L258.653 -18.251C274.242 -33.84 299.517 -33.84 315.106 -18.251L377.923 44.5661C393.512 60.1551 393.512 85.4301 377.923 101.019Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M696.956 -50.1542L650.648 -3.84605C635.059 11.743 609.784 11.743 594.195 -3.84605L547.887 -50.1542C532.298 -65.7432 532.298 -91.0182 547.887 -106.607L594.195 -152.915C609.784 -168.504 635.059 -168.504 650.648 -152.915L696.956 -106.607C712.545 -91.0172 712.545 -65.7432 696.956 -50.1542Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M758.493 103.825L712.185 150.133C696.596 165.722 671.321 165.722 655.733 150.133L609.425 103.825C593.836 88.2359 593.836 62.9608 609.425 47.3718L655.733 1.06386C671.322 -14.5251 696.597 -14.5251 712.185 1.06386L758.493 47.3718C774.082 62.9608 774.082 88.2359 758.493 103.825Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M674.716 80.202L501.67 253.248C486.081 268.837 460.806 268.837 445.217 253.248L272.171 80.202C256.582 64.613 256.582 39.338 272.171 23.749L445.217 -149.297C460.806 -164.886 486.081 -164.886 501.67 -149.297L674.716 23.75C690.305 39.339 690.305 64.613 674.716 80.202Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M579.394 334.046L523.831 389.609C508.242 405.198 482.967 405.198 467.378 389.609L411.815 334.046C396.226 318.457 396.226 293.182 411.815 277.593L467.378 222.03C482.967 206.441 508.242 206.441 523.831 222.03L579.394 277.593C594.983 293.182 594.983 318.457 579.394 334.046Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M185.618 87.2381L158.648 114.208C146.305 126.551 126.293 126.551 113.95 114.208L86.9799 87.2381C74.6369 74.8951 74.6369 54.883 86.9799 42.539L113.95 15.569C126.293 3.22605 146.305 3.22605 158.648 15.569L185.618 42.539C197.961 54.882 197.961 74.8941 185.618 87.2381Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M249.319 23.767L228.859 44.227C221.817 51.269 210.4 51.269 203.358 44.227L182.898 23.767C175.856 16.725 175.856 5.30798 182.898 -1.73402L203.358 -22.194C210.4 -29.236 221.817 -29.236 228.859 -22.194L249.319 -1.73402C256.361 5.30798 256.361 16.725 249.319 23.767Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M375.3 217.828L354.84 238.288C347.798 245.33 336.381 245.33 329.339 238.288L308.879 217.828C301.837 210.786 301.837 199.369 308.879 192.327L329.339 171.867C336.381 164.825 347.798 164.825 354.84 171.867L375.3 192.327C382.342 199.369 382.342 210.786 375.3 217.828Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M262.326 229.367L255.702 235.991C252.281 239.412 246.734 239.412 243.313 235.991L236.689 229.367C233.268 225.946 233.268 220.399 236.689 216.978L243.313 210.354C246.734 206.933 252.281 206.933 255.702 210.354L262.326 216.978C265.747 220.399 265.747 225.946 262.326 229.367Z"
                      stroke="#089df1"
                      strokeWidth={2}
                      strokeMiterlimit={10}
                    />
                    <path
                      d="M403.998 311.555L372.211 343.342C361.79 353.763 344.894 353.763 334.473 343.342L302.686 311.555C292.265 301.134 292.265 284.238 302.686 273.817L334.473 242.03C344.894 231.609 361.79 231.609 372.211 242.03L403.998 273.817C414.419 284.238 414.419 301.134 403.998 311.555Z"
                      fill="#089df1"
                    />
                    <path
                      d="M417.828 85.0572L355.011 147.874C339.422 163.463 314.147 163.463 298.558 147.874L235.741 85.0572C220.152 69.4682 220.152 44.1931 235.741 28.6051L298.558 -34.2119C314.147 -49.8009 339.422 -49.8009 355.011 -34.2119L417.828 28.6051C433.417 44.1931 433.417 69.4682 417.828 85.0572Z"
                      fill="#7b70ef"
                    />
                    <path
                      d="M714.621 64.24L541.575 237.286C525.986 252.875 500.711 252.875 485.122 237.286L312.076 64.24C296.487 48.651 296.487 23.376 312.076 7.787L485.122 -165.259C500.711 -180.848 525.986 -180.848 541.575 -165.259L714.621 7.787C730.21 23.377 730.21 48.651 714.621 64.24Z"
                      fill="#f9bf59"
                    />
                    <path
                      d="M619.299 318.084L563.736 373.647C548.147 389.236 522.872 389.236 507.283 373.647L451.72 318.084C436.131 302.495 436.131 277.22 451.72 261.631L507.283 206.068C522.872 190.479 548.147 190.479 563.736 206.068L619.299 261.631C634.888 277.221 634.888 302.495 619.299 318.084Z"
                      fill="#089df1"
                    />
                    <path
                      d="M225.523 71.276L198.553 98.2459C186.21 110.589 166.198 110.589 153.854 98.2459L126.884 71.276C114.541 58.933 114.541 38.921 126.884 26.578L153.854 -0.392014C166.197 -12.735 186.209 -12.735 198.553 -0.392014L225.523 26.578C237.866 38.92 237.866 58.932 225.523 71.276Z"
                      fill="#f7577e"
                    />
                    <path
                      d="M289.224 7.80493L268.764 28.2649C261.722 35.3069 250.305 35.3069 243.263 28.2649L222.803 7.80493C215.761 0.762926 215.761 -10.6542 222.803 -17.6962L243.263 -38.1561C250.305 -45.1981 261.722 -45.1981 268.764 -38.1561L289.224 -17.6962C296.266 -10.6542 296.266 0.762926 289.224 7.80493Z"
                      fill="#f7577e"
                    />
                    <path
                      d="M415.205 201.866L394.745 222.326C387.703 229.368 376.286 229.368 369.244 222.326L348.784 201.866C341.742 194.824 341.742 183.407 348.784 176.365L369.244 155.905C376.286 148.863 387.703 148.863 394.745 155.905L415.205 176.365C422.247 183.407 422.247 194.824 415.205 201.866Z"
                      fill="#f7577e"
                    />
                    <path
                      d="M302.231 213.405L295.607 220.029C292.186 223.45 286.639 223.45 283.218 220.029L276.594 213.405C273.173 209.984 273.173 204.437 276.594 201.016L283.218 194.392C286.639 190.971 292.186 190.971 295.607 194.392L302.231 201.016C305.652 204.437 305.652 209.984 302.231 213.405Z"
                      fill="#f7577e"
                    />
                  </g>
                </g>
                <defs>
                  <clipPath id="clip0_948_1464">
                    <rect width={560} height={600} fill="white" transform="matrix(0 -1 1 0 0 560)" />
                  </clipPath>
                </defs>
              </svg>
            </div>
            <CardBody className="min-vh-100 d-flex flex-column justify-content-center">
              <div className="d-flex justify-content-end mb-2">
                <LanguageDropdown />
              </div>
              <div className="auth-brand text-center mb-0">
                <AppLogo />
              </div>
              <div className="mt-auto">
                <h3 className="text-center">{t('auth.register.title')}</h3>
                <p className="text-muted text-center auth-sub-text mx-auto">{t('auth.register.subtitle')}</p>

                {/* eslint-disable-next-line react-hooks/refs */}
                <form className="mt-4" onSubmit={handleSubmit(onSubmit)}>
                  <Row className="mb-3">
                    <Col md={6}>
                      <FormLabel htmlFor="firstName">
                        {t('auth.register.firstNameLabel')} <span className="text-danger">*</span>
                      </FormLabel>
                      <div className="app-search">
                        <FormControl
                          type="text"
                          id="firstName"
                          placeholder={t('auth.register.firstNamePlaceholder')}
                          {...register('firstName')}
                        />
                        <LuCircleUser className="app-search-icon text-muted" />
                      </div>
                    </Col>
                    <Col md={6}>
                      <FormLabel htmlFor="lastName">
                        {t('auth.register.lastNameLabel')} <span className="text-danger">*</span>
                      </FormLabel>
                      <div className="app-search">
                        <FormControl
                          type="text"
                          id="lastName"
                          placeholder={t('auth.register.lastNamePlaceholder')}
                          {...register('lastName')}
                        />
                        <LuCircleUser className="app-search-icon text-muted" />
                      </div>
                    </Col>
                  </Row>
                  <div className="mb-3">
                    <FormLabel htmlFor="userEmail">
                      {t('auth.register.emailLabel')} <span className="text-danger">*</span>
                    </FormLabel>
                    <div className="app-search">
                      <FormControl
                        type="email"
                        id="userEmail"
                        placeholder={t('auth.register.emailPlaceholder')}
                        {...register('email')}
                      />
                      <LuMail className="app-search-icon text-muted" />
                    </div>
                  </div>
                  <div className="mb-3" data-password="bar">
                    <div className="app-search">
                      <PasswordInputWithStrength
                        id="userPassword"
                        label={t('auth.register.passwordLabel')}
                        password={password}
                        setPassword={(val) => {
                          setPassword(val);
                          setValue('password', val);
                        }}
                        placeholder={t('auth.register.passwordPlaceholder')}
                        showIcon={true}
                      />
                    </div>
                  </div>
                  <div className="mb-3">
                    <FormLabel htmlFor="confirmPassword">
                      {t('auth.register.confirmPasswordLabel')} <span className="text-danger">*</span>
                    </FormLabel>
                    <div className="app-search">
                      <FormControl
                        type="password"
                        id="confirmPassword"
                        placeholder={t('auth.register.confirmPasswordPlaceholder')}
                        {...register('confirmPassword')}
                        value={confirmPassword}
                        onChange={(e) => {
                          setConfirmPassword(e.target.value);
                          setValue('confirmPassword', e.target.value);
                        }}
                      />
                      <LuLock className="app-search-icon text-muted" />
                    </div>
                  </div>
                  <div className="d-flex justify-content-between align-items-center mb-3">
                    <div>
                      <div className="form-check">
                        {termsDocuments.TermsOfService ? (
                          <>
                            <input
                              className="form-check-input form-check-input-light fs-14"
                              type="checkbox"
                              id="termsOfServiceAccepted"
                              {...register('termsAccepted')}
                            />
                            <label className="form-check-label" htmlFor="termsOfServiceAccepted">
                              {t('auth.register.termsOfServiceAgreement').split(t('auth.register.termsOfServiceLink'))[0]}
                              <span
                                className="text-primary text-decoration-underline cursor-pointer"
                                onClick={(e) => {
                                  e.preventDefault();
                                  openTermsModal('TermsOfService');
                                }}
                                style={{ cursor: 'pointer' }}
                              >
                                {t('auth.register.termsOfServiceLink')}
                              </span>
                              {t('auth.register.termsOfServiceAgreement').split(t('auth.register.termsOfServiceLink'))[1]}
                            </label>
                          </>
                        ) : null}
                      </div>
                      {termsDocuments.PrivacyPolicy ? (
                        <div className="form-check mt-2">
                          <input
                            className="form-check-input form-check-input-light fs-14"
                            type="checkbox"
                            id="privacyPolicyAccepted"
                            {...register('privacyPolicyAccepted')}
                          />
                          <label className="form-check-label" htmlFor="privacyPolicyAccepted">
                            {t('auth.register.privacyPolicyAgreement').split(t('auth.register.privacyPolicyLink'))[0]}
                            <span
                              className="text-primary text-decoration-underline cursor-pointer"
                              onClick={(e) => {
                                e.preventDefault();
                                openTermsModal('PrivacyPolicy');
                              }}
                              style={{ cursor: 'pointer' }}
                            >
                              {t('auth.register.privacyPolicyLink')}
                            </span>
                            {t('auth.register.privacyPolicyAgreement').split(t('auth.register.privacyPolicyLink'))[1]}
                          </label>
                        </div>
                      ) : null}
                    </div>
                  </div>
                  <div className="d-grid">
                    <button type="submit" className="btn btn-primary fw-semibold py-2" disabled={isSubmitting || isSettingsLoading}>
                      {isSubmitting ? (
                        <>
                          <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                          {t('common.loading')}
                        </>
                      ) : t('auth.register.submitButton')}
                    </button>
                  </div>
                </form>
              </div>
              <p className="text-muted text-center mt-4 mb-0">
                {t('auth.register.alreadyHaveAccount')} {' '}
                <Link to="/auth/login" className="text-decoration-underline link-offset-3 fw-semibold">
                  {t('auth.register.loginLink')}
                </Link>
              </p>
              <p className="text-center text-muted mt-auto mb-0">
                © {currentYear} {applicationName} — by <span className="fw-semibold">{author}</span>
              </p>
            </CardBody>
          </Card>
        </Col>
        <Col>
          <div className="h-100 position-relative card-side-img rounded-0 overflow-hidden">
            <div className="p-4 card-img-overlay auth-overlay d-flex align-items-end justify-content-center"></div>
          </div>
        </Col>
      </Row>

      {/* Terms and Conditions Modal */}
      <Modal show={showTermsModal} onHide={() => setShowTermsModal(false)} size="lg" centered>
        <Modal.Header closeButton className="border-bottom">
          <Modal.Title className="fw-semibold">
            {activeTermsModalTitle}
          </Modal.Title>
        </Modal.Header>
        <Modal.Body style={{ maxHeight: '60vh', overflowY: 'auto' }} className="p-4">
          {isLoadingTerms ? (
            <div className="text-center p-5">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">{t('common.loading')}</span>
              </div>
            </div>
          ) : activeTerms ? (
            <div className="terms-content">
              <div dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(activeTerms.content) }} />
              <div className="mt-3 text-muted small">
                {t('auth.register.termsVersion')}: {activeTerms.version} | {t('auth.register.termsDate')}: {new Date(activeTerms.effectiveDate).toLocaleDateString()}
              </div>
            </div>
          ) : (
            <div className="text-center text-muted p-4">
              {t('auth.register.termsNotFound')}
            </div>
          )}
        </Modal.Body>
        <Modal.Footer className="border-top">
          <Button variant="light" onClick={() => setShowTermsModal(false)}>
            {t('auth.register.termsModalClose')}
          </Button>
        </Modal.Footer>
      </Modal>
    </div>
  )
}

export default Index
