import { Link, useNavigate } from 'react-router-dom'
import { Button, Form, FormControl, FormLabel, Alert } from 'react-bootstrap'
import { LuMail } from 'react-icons/lu'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { AuthService } from '@/services/authService'
import SystemSettingsService from '@/services/systemSettingsService'
import type { ForgotPasswordRequest } from '@/types/auth'
import type { AuthSystemSettings } from '@/types/systemSettings'
import { useEffect, useState } from 'react'
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

const buildResetExpiryStorageKey = (email: string) =>
  `password-reset-code-expiry:${email.trim().toLowerCase()}`
const buildResetCooldownStorageKey = (email: string) =>
  `password-reset-code-cooldown:${email.trim().toLowerCase()}`
const buildResetEmailStorageKey = () => 'password-reset-email'

const Index = () => {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [authSettings, setAuthSettings] = useState<AuthSystemSettings>(DEFAULT_AUTH_SETTINGS)
  const [isSettingsLoading, setIsSettingsLoading] = useState(true)
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<ForgotPasswordRequest>()
  const isForgotPasswordEnabled = authSettings.forgotPasswordEnabled

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

  const onSubmit = async (data: ForgotPasswordRequest) => {
    if (!isForgotPasswordEnabled) {
      return
    }

    setSuccessMessage(null)
    try {
      const response = await AuthService.forgotPassword(data)
      if (response.success) {
        setSuccessMessage(response.message || t('auth.forgotPassword.successMessage'))
        const normalizedEmail = data.email.trim()
        const expiresAt = Date.now() + authSettings.emailVerificationCodeExpirySeconds * 1000
        const cooldownUntil = Date.now() + authSettings.emailVerificationResendCooldownSeconds * 1000
        sessionStorage.setItem(buildResetEmailStorageKey(), normalizedEmail)
        sessionStorage.setItem(buildResetExpiryStorageKey(normalizedEmail), expiresAt.toString())
        sessionStorage.setItem(buildResetCooldownStorageKey(normalizedEmail), cooldownUntil.toString())
        navigate('/reset-password')
      }
    } catch (err: any) {
      // Handled by global interceptor
    }
  }

  return (
    <AuthShell
      footer={(
        <p className="text-muted text-center mt-4 mb-0">
          {t('auth.forgotPassword.backToLoginPreLink')}
          <Link to="/auth/login" className="text-decoration-underline link-offset-3 fw-semibold">
            {t('auth.forgotPassword.backToLogin')}
          </Link>
        </p>
      )}>
      <h3 className="text-center">{t('auth.forgotPassword.title')}</h3>
      <p className="text-muted text-center auth-sub-text mx-auto">
        {t('auth.forgotPassword.subtitle')}
      </p>

                {!isSettingsLoading && !isForgotPasswordEnabled && (
                  <Alert variant="warning">{t('auth.forgotPassword.disabledMessage')}</Alert>
                )}

                {successMessage && <Alert variant="success">{successMessage}</Alert>}

                <Form className="mt-4" onSubmit={handleSubmit(onSubmit)}>
                  <div className="mb-3">
                    <FormLabel htmlFor="userEmail">
                      {t('auth.forgotPassword.emailLabel')} <span className="text-danger">*</span>
                    </FormLabel>
                    <div className="app-search">
                      <FormControl
                        type="email"
                        id="userEmail"
                        placeholder={t('auth.forgotPassword.emailPlaceholder')}
                        disabled={!isForgotPasswordEnabled || isSettingsLoading}
                        {...register('email')}
                      />
                      <LuMail className="app-search-icon text-muted" />
                    </div>
                  </div>

                  <div className="d-grid">
                    <Button
                      variant="primary"
                      type="submit"
                      className="fw-semibold py-2"
                      disabled={isSubmitting || !isForgotPasswordEnabled || isSettingsLoading}>
                      {isSubmitting ? t('common.loading') : t('auth.forgotPassword.submitButton')}
                    </Button>
                  </div>
                </Form>
    </AuthShell>
  )
}

export default Index
