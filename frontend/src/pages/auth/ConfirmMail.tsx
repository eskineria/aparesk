import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Alert, Button, FormControl, FormLabel } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuInfo, LuMail } from 'react-icons/lu'

import { AuthService } from '@/services/authService'
import SystemSettingsService from '@/services/systemSettingsService'
import type { AuthSystemSettings } from '@/types/systemSettings'
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

const buildConfirmEmailStorageKey = () => 'confirm-email-address'
const buildConfirmExpiryStorageKey = (email: string) =>
  `confirm-email-expiry:${email.trim().toLowerCase()}`
const buildConfirmCooldownStorageKey = (email: string) =>
  `confirm-email-cooldown:${email.trim().toLowerCase()}`

const Index = () => {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const emptyCodeDigits = () => Array.from({ length: 6 }, () => '')
  const [searchParams] = useSearchParams()
  const prefilledEmail = searchParams.get('email') ?? ''
  const source = searchParams.get('from')
  const codeInputRefs = useRef<Array<HTMLInputElement | null>>([])
  const lastAutoSubmitRef = useRef('')

  const [email, setEmail] = useState(() => {
    const queryEmail = prefilledEmail.trim()
    const storedEmail = sessionStorage.getItem(buildConfirmEmailStorageKey())?.trim() ?? ''
    return queryEmail || storedEmail
  })
  const [isEmailLocked, setIsEmailLocked] = useState(
    prefilledEmail.trim().length > 0 || (sessionStorage.getItem(buildConfirmEmailStorageKey())?.trim().length ?? 0) > 0,
  )
  const [codeDigits, setCodeDigits] = useState<string[]>(emptyCodeDigits)
  const [errorMessage, setErrorMessage] = useState('')
  const [infoMessage, setInfoMessage] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isResending, setIsResending] = useState(false)
  const [isTokenVerifying] = useState(false)
  const [cooldownSeconds, setCooldownSeconds] = useState(0)
  const [remainingSeconds, setRemainingSeconds] = useState(0)
  const [hasExpiryState, setHasExpiryState] = useState(false)
  const [status, setStatus] = useState<'form' | 'success' | 'already_confirmed'>('form')
  const [authSettings, setAuthSettings] = useState<AuthSystemSettings>(DEFAULT_AUTH_SETTINGS)
  const [isSettingsLoading, setIsSettingsLoading] = useState(true)

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
    const queryEmail = prefilledEmail.trim()
    if (!queryEmail) {
      return
    }

    sessionStorage.setItem(buildConfirmEmailStorageKey(), queryEmail)
    setEmail(queryEmail)
    setIsEmailLocked(true)
    navigate(source ? `/confirm-email?from=${encodeURIComponent(source)}` : '/confirm-email', { replace: true })
  }, [navigate, prefilledEmail, source])

  useEffect(() => {
    if (!email.trim()) {
      return
    }

    sessionStorage.setItem(buildConfirmEmailStorageKey(), email.trim())
  }, [email])



  useEffect(() => {
    if (source !== 'login' || !email.trim()) {
      return
    }

    setInfoMessage(t('auth.confirmEmail.loginContinueHint'))
  }, [source, email, t])

  useEffect(() => {
    if (!email.trim()) {
      return
    }

    const storageKey = buildConfirmCooldownStorageKey(email)
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
    if (!email.trim()) {
      return
    }

    const storageKey = buildConfirmExpiryStorageKey(email)
    const updateRemainingSeconds = () => {
      const storedValue = sessionStorage.getItem(storageKey)
      const expiresAt = Number(storedValue)
      if (!expiresAt) {
        setHasExpiryState(false)
        setRemainingSeconds(0)
        return
      }

      setHasExpiryState(true)
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
  }, [email])

  useEffect(() => {
    if (!hasExpiryState || remainingSeconds > 0) {
      return
    }

    if (codeDigits.every((digit) => digit === '')) {
      return
    }

    setCodeDigits(emptyCodeDigits())
    lastAutoSubmitRef.current = ''
    setErrorMessage(t('auth.confirmEmail.codeExpiredMessage'))
  }, [codeDigits, hasExpiryState, remainingSeconds, t])

  const isAlreadyConfirmedMessage = (message: string) => {
    const normalized = message.toLowerCase()
    return (
      normalized.includes('already confirmed') ||
      normalized.includes('already verified') ||
      normalized.includes('zaten onaylanmış') ||
      normalized.includes('zaten onaylı')
    )
  }

  const isExpiredMessage = (message: string) => {
    const normalized = message.toLowerCase()
    return normalized.includes('expired') || normalized.includes('süresi doldu')
  }

  const resendWaitSeconds = (message: string) => {
    const matched = message.match(/(\d+)/)
    return matched ? Number(matched[1]) : 0
  }

  const isFormReady = useMemo(() => {
    return email.trim().length > 0 && codeDigits.every((digit) => digit.length === 1)
  }, [email, codeDigits])

  const handleCodeChange = (index: number, value: string) => {
    const currentDigit = codeDigits[index]
    const newVal = value.replace(/\D/g, '')

    let nextDigit = ''
    if (newVal.length > 1 && currentDigit) {
      // If the user typed a new digit while one was already there,
      // find the newly added digit (handle both cursor at start and end)
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

    setIsSubmitting(true)
    try {
      const response = await AuthService.confirmEmail({ email, code })
      if (response.success) {
        sessionStorage.removeItem(buildConfirmEmailStorageKey())
        sessionStorage.removeItem(buildConfirmExpiryStorageKey(email))
        sessionStorage.removeItem(buildConfirmCooldownStorageKey(email))
        setStatus('success')
        return
      }

      if (isAlreadyConfirmedMessage(response.message)) {
        sessionStorage.removeItem(buildConfirmEmailStorageKey())
        sessionStorage.removeItem(buildConfirmExpiryStorageKey(email))
        sessionStorage.removeItem(buildConfirmCooldownStorageKey(email))
        setStatus('already_confirmed')
        return
      }

      if (isExpiredMessage(response.message)) {
        setCodeDigits(emptyCodeDigits())
        lastAutoSubmitRef.current = ''
        codeInputRefs.current[0]?.focus()
      }

      setErrorMessage(response.message || t('auth.confirmEmail.errorMessage'))
    } catch (error: any) {
      const message = error?.response?.data?.message || error?.message || t('auth.errors.default')
      if (isAlreadyConfirmedMessage(message)) {
        sessionStorage.removeItem(buildConfirmEmailStorageKey())
        sessionStorage.removeItem(buildConfirmExpiryStorageKey(email))
        sessionStorage.removeItem(buildConfirmCooldownStorageKey(email))
        setStatus('already_confirmed')
        return
      }

      if (isExpiredMessage(message)) {
        setCodeDigits(emptyCodeDigits())
        lastAutoSubmitRef.current = ''
        codeInputRefs.current[0]?.focus()
      }

      setErrorMessage(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  useEffect(() => {
    if (!isFormReady || isTokenVerifying || isSubmitting || status !== 'form') {
      if (!isFormReady) {
        lastAutoSubmitRef.current = ''
      }
      return
    }

    const code = codeDigits.join('')
    const attemptKey = `${email.trim().toLowerCase()}|${code}`
    if (attemptKey === lastAutoSubmitRef.current) {
      return
    }

    lastAutoSubmitRef.current = attemptKey
    void verifyCode(code)
  }, [codeDigits, email, isFormReady, isSubmitting, isTokenVerifying, status])

  const onConfirm = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!isFormReady || isTokenVerifying || isSubmitting) {
      return
    }

    const code = codeDigits.join('')
    lastAutoSubmitRef.current = `${email.trim().toLowerCase()}|${code}`
    await verifyCode(code)
  }

  const onResend = async () => {
    setErrorMessage('')
    setInfoMessage('')

    setIsResending(true)
    try {
      const response = await AuthService.resendConfirmationCode({ email })
      if (response.success) {
        setInfoMessage(response.message || t('auth.confirmEmail.codeResent'))
        setIsEmailLocked(true)
        const expiresAt = Date.now() + authSettings.emailVerificationCodeExpirySeconds * 1000
        const cooldownUntil = Date.now() + authSettings.emailVerificationResendCooldownSeconds * 1000
        sessionStorage.setItem(buildConfirmExpiryStorageKey(email), expiresAt.toString())
        sessionStorage.setItem(buildConfirmCooldownStorageKey(email), cooldownUntil.toString())
        setRemainingSeconds(authSettings.emailVerificationCodeExpirySeconds)
        setCooldownSeconds(authSettings.emailVerificationResendCooldownSeconds)
        setCodeDigits(emptyCodeDigits())
        lastAutoSubmitRef.current = ''
        codeInputRefs.current[0]?.focus()
        return
      }

      const wait = resendWaitSeconds(response.message || '')
      if (wait > 0) {
        setIsEmailLocked(true)
        sessionStorage.setItem(buildConfirmCooldownStorageKey(email), (Date.now() + wait * 1000).toString())
        setCooldownSeconds(wait)
      }

      if (isAlreadyConfirmedMessage(response.message)) {
        setStatus('already_confirmed')
        return
      }

      setErrorMessage(response.message || t('auth.confirmEmail.errorMessage'))
    } catch (error: any) {
      const message = error?.response?.data?.message || error?.message || t('auth.errors.default')
      const wait = resendWaitSeconds(message)
      if (wait > 0) {
        setIsEmailLocked(true)
        sessionStorage.setItem(buildConfirmCooldownStorageKey(email), (Date.now() + wait * 1000).toString())
        setCooldownSeconds(wait)
      }

      if (isAlreadyConfirmedMessage(message)) {
        setStatus('already_confirmed')
        return
      }

      setErrorMessage(message)
    } finally {
      setIsResending(false)
    }
  }

  if (!isSettingsLoading && !authSettings.emailVerificationRequired) {
    return (
      <AuthShell bodyClassName="text-center p-5" contentClassName="">
        <h4 className="fw-bold mb-3">{t('auth.confirmEmail.notRequiredTitle')}</h4>
        <p className="text-muted mb-4">{t('auth.confirmEmail.notRequiredMessage')}</p>
        <div className="d-grid">
          <Link to="/auth/login" className="btn btn-primary fw-semibold py-2">
            {t('auth.confirmEmail.backToLogin')}
          </Link>
        </div>
      </AuthShell>
    )
  }

  return (
    <AuthShell>
      {(status === 'success' || status === 'already_confirmed') && (
        <>
          <div className="mb-4">
            <div className="avatar-xxl mx-auto mt-2">
              <div className={`avatar-title border border-dashed rounded-circle ${status === 'success' ? 'bg-soft-success text-success border-success' : 'bg-soft-primary text-primary border-primary'}`}>
                {status === 'success' ? <LuMail size={64} /> : <LuInfo size={64} />}
              </div>
            </div>
          </div>
          <h4 className="fw-bold text-center mb-2">
            {status === 'success' ? t('auth.confirmEmail.successTitle') : t('auth.confirmEmail.alreadyConfirmedTitle')}
          </h4>
          <p className="text-muted text-center mb-4">
            {status === 'success' ? t('auth.confirmEmail.successMessage') : t('auth.confirmEmail.alreadyConfirmedMessage')}
          </p>
          <div className="d-grid">
            <Link to="/auth/login" className="btn btn-primary fw-semibold py-2">
              {status === 'success' ? t('auth.confirmEmail.backToDashboard') : t('auth.confirmEmail.alreadyConfirmedButton')}
            </Link>
          </div>
        </>
      )}

      {status === 'form' && isTokenVerifying && (
        <>
          <h4 className="fw-bold text-center mb-3">{t('auth.confirmEmail.verifying')}</h4>
          <p className="text-muted text-center">{t('auth.confirmEmail.tokenVerificationInProgress')}</p>
        </>
      )}

      {status === 'form' && !isTokenVerifying && (
        <form className="mt-4" onSubmit={onConfirm}>
          <h3 className="text-center">{t('auth.confirmEmail.title')}</h3>
          <p className="text-muted text-center auth-sub-text mx-auto">{t('auth.confirmEmail.subtitle')}</p>

          {errorMessage && <Alert variant="danger">{errorMessage}</Alert>}
          {infoMessage && <Alert variant="success">{infoMessage}</Alert>}

          <div className="mb-3">
            <FormLabel htmlFor="confirm-email-email">{t('auth.confirmEmail.emailLabel')}</FormLabel>
            <FormControl
              id="confirm-email-email"
              type="email"
              value={email}
              onChange={(event) => {
                if (!isEmailLocked) {
                  setEmail(event.target.value)
                }
              }}
              placeholder={t('auth.confirmEmail.emailPlaceholder')}
              autoComplete="email"
              readOnly={isEmailLocked}
            />
          </div>

          <div className="mb-3">
            <FormLabel htmlFor="confirm-email-code-0">{t('auth.confirmEmail.codeLabel')}</FormLabel>
            <div className="d-flex justify-content-center gap-2">
              {codeDigits.map((digit, index) => (
                <FormControl
                  key={`confirm-code-${index}`}
                  id={`confirm-email-code-${index}`}
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
            {hasExpiryState && (
              <div className="text-muted text-center small mt-2">
                {remainingSeconds > 0
                  ? t('auth.confirmEmail.codeExpiresIn', { seconds: remainingSeconds })
                  : t('auth.confirmEmail.codeExpiredMessage')}
              </div>
            )}
          </div>

          {isSubmitting && (
            <p className="text-center text-muted mb-3">{t('auth.confirmEmail.verifying')}</p>
          )}

          <div className="d-grid gap-2 mt-4">
            <Button
              type="button"
              variant="outline-secondary"
              className="fw-semibold py-2"
              disabled={isResending || isSubmitting || cooldownSeconds > 0 || !email.trim()}
              onClick={onResend}>
              {cooldownSeconds > 0
                ? t('auth.confirmEmail.resendCountdown', { seconds: cooldownSeconds })
                : t('auth.confirmEmail.resendButton')}
            </Button>
          </div>
        </form>
      )}
    </AuthShell>
  )
}

export default Index
