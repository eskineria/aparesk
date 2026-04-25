import { useEffect, useMemo, useRef, useState } from 'react'
import { Alert, Button, FormControl, FormLabel } from 'react-bootstrap'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { LuCheck, LuInfo, LuLock } from 'react-icons/lu'

import { AuthService } from '@/services/authService'
import SystemSettingsService from '@/services/systemSettingsService'
import type { AuthSystemSettings } from '@/types/systemSettings'
import type { ResetPasswordRequest } from '@/types/auth'
import AuthShell from './components/AuthShell'

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

const emptyCodeDigits = () => Array.from({ length: 6 }, () => '')
const buildResetExpiryStorageKey = (email: string) =>
  `password-reset-code-expiry:${email.trim().toLowerCase()}`
const buildResetCooldownStorageKey = (email: string) =>
  `password-reset-code-cooldown:${email.trim().toLowerCase()}`
const buildResetEmailStorageKey = () => 'password-reset-email'

const ResetPassword = () => {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [email, setEmail] = useState(() => {
    const queryEmail = (searchParams.get('email') ?? '').trim()
    const storedEmail = sessionStorage.getItem(buildResetEmailStorageKey())?.trim() ?? ''
    return queryEmail || storedEmail
  })
  const codeInputRefs = useRef<Array<HTMLInputElement | null>>([])
  const lastAutoSubmitRef = useRef('')

  const [status, setStatus] = useState<'code' | 'form' | 'success' | 'error'>(
    email ? 'code' : 'error',
  )
  const [codeDigits, setCodeDigits] = useState<string[]>(emptyCodeDigits)
  const [verifiedCode, setVerifiedCode] = useState('')
  const [errorMessage, setErrorMessage] = useState('')
  const [infoMessage, setInfoMessage] = useState('')
  const [isVerifyingCode, setIsVerifyingCode] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isResending, setIsResending] = useState(false)
  const [cooldownSeconds, setCooldownSeconds] = useState(0)
  const [remainingSeconds, setRemainingSeconds] = useState(0)
  const [authSettings, setAuthSettings] = useState<AuthSystemSettings>(DEFAULT_AUTH_SETTINGS)
  const [isSettingsLoading, setIsSettingsLoading] = useState(true)

  const { register, handleSubmit, formState: { errors } } = useForm<ResetPasswordRequest>({
    defaultValues: {
      email: email || '',
      code: '',
      newPassword: '',
      confirmNewPassword: '',
    },
  })

  useEffect(() => {
    const queryEmail = (searchParams.get('email') ?? '').trim()
    if (!queryEmail) {
      return
    }

    sessionStorage.setItem(buildResetEmailStorageKey(), queryEmail)
    setEmail(queryEmail)
    navigate('/reset-password', { replace: true })
  }, [navigate, searchParams])

  useEffect(() => {
    if (!email) {
      return
    }

    sessionStorage.setItem(buildResetEmailStorageKey(), email)
  }, [email])

  useEffect(() => {
    const loadAuthSettings = async () => {
      setIsSettingsLoading(true)
      try {
        const response = await SystemSettingsService.getPublicAuthSettings()
        if (response.success && response.data) {
          setAuthSettings(response.data)
        }
      } catch {
        // Global axios interceptor handles errors.
      } finally {
        setIsSettingsLoading(false)
      }
    }

    void loadAuthSettings()
  }, [])

  useEffect(() => {
    if (!email) {
      return
    }

    const storageKey = buildResetCooldownStorageKey(email)
    const updateCooldownSeconds = () => {
      const storedValue = sessionStorage.getItem(storageKey)
      const cooldownUntil = Number(storedValue)
      if (!cooldownUntil) {
        setCooldownSeconds(0)
        return
      }

      // eslint-disable-next-line react-hooks/purity
      const nextRemaining = Math.max(0, Math.ceil((cooldownUntil - Date.now()) / 1000))
      setCooldownSeconds(nextRemaining)
    }

    updateCooldownSeconds()

    if (!sessionStorage.getItem(storageKey)) {
      return
    }

    const timer = window.setInterval(updateCooldownSeconds, 1000)
    return () => window.clearInterval(timer)
  }, [email])

  useEffect(() => {
    if (!email) {
      return
    }

    const storageKey = buildResetExpiryStorageKey(email)
    const updateRemainingSeconds = () => {
      const storedValue = sessionStorage.getItem(storageKey)
      const expiresAt = Number(storedValue)
      if (!expiresAt) {
        setRemainingSeconds(0)
        return
      }

      // eslint-disable-next-line react-hooks/purity
      const nextRemaining = Math.max(0, Math.ceil((expiresAt - Date.now()) / 1000))
      setRemainingSeconds(nextRemaining)
    }

    updateRemainingSeconds()

    if (!sessionStorage.getItem(storageKey)) {
      return
    }

    const timer = window.setInterval(updateRemainingSeconds, 1000)
    return () => window.clearInterval(timer)
  }, [email, status])

  useEffect(() => {
    if (remainingSeconds > 0 || status === 'success') {
      return
    }

    if (!verifiedCode && codeDigits.every((digit) => digit === '')) {
      return
    }

    setVerifiedCode('')
    setCodeDigits(emptyCodeDigits())
    lastAutoSubmitRef.current = ''
    setStatus('code')
    setErrorMessage(t('auth.resetPassword.codeExpiredMessage'))
  }, [codeDigits, remainingSeconds, status, t, verifiedCode])

  const isExpiredMessage = (message: string) => {
    const normalized = message.toLowerCase()
    return normalized.includes('expired') || normalized.includes('süresi doldu')
  }

  const resendWaitSeconds = (message: string) => {
    const matched = message.match(/(\d+)/)
    return matched ? Number(matched[1]) : 0
  }

  const isCodeReady = useMemo(() => {
    return email.length > 0 && codeDigits.every((digit) => digit.length === 1)
  }, [codeDigits, email])

  const handleCodeChange = (index: number, value: string) => {
    const currentDigit = codeDigits[index]
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

    setCodeDigits((prev) => {
      const next = [...prev]
      next[index] = nextDigit
      return next
    })

    if (nextDigit && index < 5) {
      codeInputRefs.current[index + 1]?.focus()
    }
  }

  const handleCodeKeyDown = (index: number, event: React.KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    if (event.key === 'Backspace') {
      if (codeDigits[index]) {
        setCodeDigits((prev) => {
          const next = [...prev]
          next[index] = ''
          return next
        })
        return
      }

      if (index > 0) {
        setCodeDigits((prev) => {
          const next = [...prev]
          next[index - 1] = ''
          return next
        })
        codeInputRefs.current[index - 1]?.focus()
      }
    }

    if (event.key === 'ArrowLeft' && index > 0) {
      codeInputRefs.current[index - 1]?.focus()
    }

    if (event.key === 'ArrowRight' && index < 5) {
      codeInputRefs.current[index + 1]?.focus()
    }
  }

  const handleCodePaste = (event: React.ClipboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const pastedDigits = event.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6)
    if (!pastedDigits) {
      return
    }

    event.preventDefault()

    const next = emptyCodeDigits()
    pastedDigits.split('').forEach((digit, index) => {
      next[index] = digit
    })

    setCodeDigits(next)

    const focusIndex = Math.min(pastedDigits.length, 6) - 1
    if (focusIndex >= 0) {
      codeInputRefs.current[focusIndex]?.focus()
    }
  }

  const verifyCode = async (code: string) => {
    setErrorMessage('')
    setInfoMessage('')
    setIsVerifyingCode(true)

    try {
      const response = await AuthService.verifyResetPasswordCode({ email, code })
      if (response.success) {
        setVerifiedCode(code)
        setStatus('form')
        return
      }

      if (isExpiredMessage(response.message || '')) {
        setCodeDigits(emptyCodeDigits())
        lastAutoSubmitRef.current = ''
        codeInputRefs.current[0]?.focus()
      }

      setErrorMessage(response.message || t('auth.resetPassword.codeError'))
    } catch (error: any) {
      const message = error?.response?.data?.message || error?.message || t('auth.errors.default')

      if (isExpiredMessage(message)) {
        setCodeDigits(emptyCodeDigits())
        lastAutoSubmitRef.current = ''
        codeInputRefs.current[0]?.focus()
      }

      setErrorMessage(message)
    } finally {
      setIsVerifyingCode(false)
    }
  }

  useEffect(() => {
    if (!isCodeReady || isVerifyingCode || status !== 'code') {
      if (!isCodeReady) {
        lastAutoSubmitRef.current = ''
      }
      return
    }

    const code = codeDigits.join('')
    const attemptKey = `${email.toLowerCase()}|${code}`
    if (attemptKey === lastAutoSubmitRef.current) {
      return
    }

    lastAutoSubmitRef.current = attemptKey
    void verifyCode(code)
  }, [codeDigits, email, isCodeReady, isVerifyingCode, status])

  const onCodeSubmit = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!isCodeReady || isVerifyingCode) {
      return
    }

    const code = codeDigits.join('')
    lastAutoSubmitRef.current = `${email.toLowerCase()}|${code}`
    await verifyCode(code)
  }

  const onResend = async () => {
    setErrorMessage('')
    setInfoMessage('')
    setIsResending(true)

    try {
      const response = await AuthService.resendResetPasswordCode({ email })
      if (response.success) {
        setInfoMessage(response.message || t('auth.resetPassword.codeResent'))
        // eslint-disable-next-line react-hooks/purity
        const cooldownUntil = Date.now() + authSettings.emailVerificationResendCooldownSeconds * 1000
        sessionStorage.setItem(buildResetCooldownStorageKey(email), cooldownUntil.toString())
        setCooldownSeconds(authSettings.emailVerificationResendCooldownSeconds)
        // eslint-disable-next-line react-hooks/purity
        const expiresAt = Date.now() + authSettings.emailVerificationCodeExpirySeconds * 1000
        sessionStorage.setItem(buildResetExpiryStorageKey(email), expiresAt.toString())
        setRemainingSeconds(authSettings.emailVerificationCodeExpirySeconds)
        setVerifiedCode('')
        setCodeDigits(emptyCodeDigits())
        lastAutoSubmitRef.current = ''
        codeInputRefs.current[0]?.focus()
        return
      }

      const wait = resendWaitSeconds(response.message || '')
      if (wait > 0) {
        setCooldownSeconds(wait)
      }

      setErrorMessage(response.message || t('auth.resetPassword.codeError'))
    } catch (error: any) {
      const message = error?.response?.data?.message || error?.message || t('auth.errors.default')
      const wait = resendWaitSeconds(message)
      if (wait > 0) {
        setCooldownSeconds(wait)
      }

      setErrorMessage(message)
    } finally {
      setIsResending(false)
    }
  }

  const onSubmit = async (data: ResetPasswordRequest) => {
    if (!verifiedCode) {
      return
    }

    setErrorMessage('')
    setInfoMessage('')
    setIsSubmitting(true)

    try {
      const response = await AuthService.resetPassword({
        ...data,
        email,
        code: verifiedCode,
      })

      if (response.success) {
        sessionStorage.removeItem(buildResetExpiryStorageKey(email))
        sessionStorage.removeItem(buildResetCooldownStorageKey(email))
        sessionStorage.removeItem(buildResetEmailStorageKey())
        setStatus('success')
        return
      }

      setErrorMessage(response.message || t('auth.resetPassword.submitError'))
    } catch (error: any) {
      const message = error?.response?.data?.message || error?.message || t('auth.errors.default')
      setErrorMessage(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthShell>
      {!isSettingsLoading && !authSettings.forgotPasswordEnabled && (
        <Alert variant="warning">{t('auth.forgotPassword.disabledMessage')}</Alert>
      )}

      {status === 'error' && (
        <>
          <div className="mb-4">
            <div className="avatar-xxl mx-auto mt-2">
              <div className="avatar-title border border-dashed rounded-circle bg-soft-danger text-danger border-danger">
                <LuInfo size={64} />
              </div>
            </div>
          </div>
          <h4 className="fw-bold text-center mb-2">{t('auth.resetPassword.invalidLinkTitle')}</h4>
          <p className="text-muted text-center mb-4">{t('auth.resetPassword.invalidLinkMessage')}</p>
          <div className="d-grid gap-2">
            <Link to="/auth/forgot-password" className="btn btn-primary fw-semibold py-2">
              {t('auth.resetPassword.requestNewLink')}
            </Link>
            <Link to="/auth/login" className="btn btn-outline-secondary fw-semibold py-2">
              {t('auth.resetPassword.backToLogin')}
            </Link>
          </div>
        </>
      )}

      {status === 'success' && (
        <>
          <div className="mb-4">
            <div className="avatar-xxl mx-auto mt-2">
              <div className="avatar-title border border-dashed rounded-circle bg-soft-success text-success border-success">
                <LuCheck size={64} />
              </div>
            </div>
          </div>
          <h4 className="fw-bold text-center mb-2">{t('auth.resetPassword.successTitle')}</h4>
          <p className="text-muted text-center mb-4">{t('auth.resetPassword.successMessage')}</p>
          <div className="d-grid">
            <Link to="/auth/login" className="btn btn-primary fw-semibold py-2">
              {t('auth.resetPassword.backToLogin')}
            </Link>
          </div>
        </>
      )}

      {status === 'code' && (
        <form className="mt-4" onSubmit={onCodeSubmit}>
          <h3 className="text-center">{t('auth.resetPassword.codeTitle')}</h3>
          <p className="text-muted text-center auth-sub-text mx-auto">{t('auth.resetPassword.codeSubtitle')}</p>

          {errorMessage && <Alert variant="danger">{errorMessage}</Alert>}
          {infoMessage && <Alert variant="success">{infoMessage}</Alert>}

          <div className="mb-3">
            <FormLabel htmlFor="resetPasswordEmail">{t('auth.forgotPassword.emailLabel')}</FormLabel>
            <FormControl id="resetPasswordEmail" type="email" value={email} readOnly />
          </div>

          <div className="mb-3">
            <FormLabel htmlFor="reset-password-code-0">{t('auth.resetPassword.codeLabel')}</FormLabel>
            <div className="d-flex justify-content-center gap-2">
              {codeDigits.map((digit, index) => (
                <FormControl
                  key={`reset-code-${index}`}
                  id={`reset-password-code-${index}`}
                  type="text"
                  inputMode="numeric"
                  maxLength={1}
                  autoComplete={index === 0 ? 'one-time-code' : 'off'}
                  value={digit}
                  onChange={(event) => handleCodeChange(index, event.target.value)}
                  onKeyDown={(event) => handleCodeKeyDown(index, event)}
                  onPaste={handleCodePaste}
                  onFocus={(event) => event.currentTarget.select()}
                  onClick={(event) => event.currentTarget.select()}
                  ref={(input) => {
                    codeInputRefs.current[index] = input
                  }}
                  className="text-center fw-semibold"
                  style={{ width: 46, fontSize: '1.2rem' }}
                />
              ))}
            </div>
            <div className="text-muted text-center small mt-2">
              {remainingSeconds > 0
                ? t('auth.resetPassword.codeExpiresIn', { seconds: remainingSeconds })
                : t('auth.resetPassword.codeExpiredMessage')}
            </div>
          </div>

          <div className="d-grid gap-2">
            <Button
              type="submit"
              variant="primary"
              className="fw-semibold py-2"
              disabled={!isCodeReady || isVerifyingCode}>
              {isVerifyingCode ? t('common.loading') : t('auth.resetPassword.verifyButton')}
            </Button>
            <Button
              type="button"
              variant="outline-secondary"
              className="fw-semibold py-2"
              onClick={onResend}
              disabled={
                isResending ||
                isVerifyingCode ||
                cooldownSeconds > 0 ||
                !authSettings.forgotPasswordEnabled
              }>
              {cooldownSeconds > 0
                ? t('auth.resetPassword.resendCountdown', { seconds: cooldownSeconds })
                : t('auth.resetPassword.resendButton')}
            </Button>
          </div>
        </form>
      )}

      {status === 'form' && (
        <form onSubmit={handleSubmit(onSubmit)} className="mt-4">
          <h3 className="text-center">{t('auth.resetPassword.title')}</h3>
          <p className="text-muted text-center auth-sub-text mx-auto">{t('auth.resetPassword.subtitle')}</p>
          <p className="text-muted text-center small mt-n2 mb-3">
            {remainingSeconds > 0
              ? t('auth.resetPassword.codeExpiresIn', { seconds: remainingSeconds })
              : t('auth.resetPassword.codeExpiredMessage')}
          </p>

          {errorMessage && <Alert variant="danger">{errorMessage}</Alert>}
          {infoMessage && <Alert variant="success">{infoMessage}</Alert>}

          <input type="hidden" value={email} {...register('email')} />
          <input type="hidden" value={verifiedCode} {...register('code')} />

          <div className="mb-3">
            <FormLabel htmlFor="newPassword">{t('auth.resetPassword.passwordLabel')}</FormLabel>
            <div className="app-search">
              <FormControl
                id="newPassword"
                type="password"
                placeholder={t('auth.resetPassword.passwordPlaceholder')}
                isInvalid={Boolean(errors.newPassword)}
                {...register('newPassword', { required: true })}
              />
              <LuLock className="app-search-icon text-muted" />
            </div>
          </div>

          <div className="mb-4">
            <FormLabel htmlFor="confirmNewPassword">{t('auth.resetPassword.confirmPasswordLabel')}</FormLabel>
            <div className="app-search">
              <FormControl
                id="confirmNewPassword"
                type="password"
                placeholder={t('auth.resetPassword.confirmPasswordPlaceholder')}
                isInvalid={Boolean(errors.confirmNewPassword)}
                {...register('confirmNewPassword', { required: true })}
              />
              <LuLock className="app-search-icon text-muted" />
            </div>
          </div>

          <div className="d-grid">
            <Button
              type="submit"
              variant="primary"
              className="fw-semibold py-2"
              disabled={isSubmitting}>
              {isSubmitting ? t('common.loading') : t('auth.resetPassword.submitButton')}
            </Button>
          </div>
        </form>
      )}
    </AuthShell>
  )
}

export default ResetPassword
