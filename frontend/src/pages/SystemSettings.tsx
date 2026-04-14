import { useEffect, useState } from 'react'
import { Button, Card, Col, Container, Form, Row, Spinner, Tab, Tabs } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuX, LuPlus } from 'react-icons/lu'
import Select, { type MultiValue } from 'react-select'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import { useBranding } from '@/context/BrandingContext'
import SystemSettingsService from '@/services/systemSettingsService'
import { AccessControlService } from '@/services/accessControlService'
import localizationService from '@/services/localizationService'
import { showToast } from '@/utils/toast'
import type { AuthSystemSettings } from '@/types/systemSettings'
import './SystemSettings.scss'

type SettingsTab =
    | 'branding'
    | 'access'
    | 'session'
    | 'security'
    | 'maintenance'
    | 'communication'
    | 'localization'
    | 'audit'

type NumberSettingKey =
    | 'emailVerificationCodeExpirySeconds'
    | 'emailVerificationResendCooldownSeconds'
    | 'sessionAccessTokenLifetimeMinutes'
    | 'sessionRefreshTokenLifetimeDays'
    | 'sessionMaxActiveSessions'
    | 'sessionIdleTimeoutMinutes'
    | 'sessionWarningBeforeTimeoutMinutes'
    | 'sessionRememberMeDurationDays'
    | 'passwordMinLength'
    | 'mfaTrustedDeviceDurationDays'
    | 'accountInactiveLockDays'
    | 'accountPasswordExpiryDays'
    | 'loginMaxFailedAttempts'
    | 'loginLockoutDurationMinutes'
    | 'emailDailySendLimit'
    | 'emailRetryMaxAttempts'
    | 'auditRetentionDays'
    | 'auditCleanupScheduleHourUtc'

type TextSettingKey =
    | 'mfaBypassIpWhitelist'
    | 'registrationAllowedEmailDomains'
    | 'registrationBlockedEmailDomains'
    | 'maintenanceMessage'
    | 'maintenanceIpWhitelist'
    | 'maintenanceRoleWhitelist'
    | 'emailSenderName'
    | 'emailSenderAddress'
    | 'notificationSecurityEmailRecipients'
    | 'localizationDefaultCulture'
    | 'localizationFallbackCulture'
    | 'applicationName'

type SelectOption = {
    label: string
    value: string
}

const DEFAULT_SETTINGS: AuthSystemSettings = {
    loginEnabled: true,
    registerEnabled: true,
    googleLoginEnabled: true,
    forgotPasswordEnabled: true,
    changePasswordEnabled: true,
    sessionManagementEnabled: true,
    emailVerificationRequired: true,
    emailVerificationCodeExpirySeconds: 180,
    emailVerificationResendCooldownSeconds: 60,
    sessionAccessTokenLifetimeMinutes: 60,
    sessionRefreshTokenLifetimeDays: 7,
    sessionMaxActiveSessions: 5,
    sessionIdleTimeoutMinutes: 60,
    sessionWarningBeforeTimeoutMinutes: 2,
    sessionRememberMeDurationDays: 30,
    sessionSingleDeviceModeEnabled: false,
    passwordMinLength: 8,
    passwordRequireUppercase: true,
    passwordRequireLowercase: true,
    passwordRequireDigit: true,
    passwordRequireNonAlphanumeric: false,
    mfaFeatureEnabled: true,
    mfaEnforceForAdmins: false,
    mfaTrustedDeviceDurationDays: 30,
    mfaBypassIpWhitelist: '',
    registrationInvitationRequired: false,
    registrationAllowedEmailDomains: '',
    registrationBlockedEmailDomains: '',
    registrationAutoApproveEnabled: true,
    accountInactiveLockDays: 90,
    accountForcePasswordChangeOnFirstLogin: false,
    accountPasswordExpiryDays: 90,
    loginLockoutEnabled: true,
    loginMaxFailedAttempts: 5,
    loginLockoutDurationMinutes: 15,
    maintenanceModeEnabled: false,
    maintenanceEndTime: null,
    maintenanceMessage: '',
    maintenanceCountdownEnabled: true,
    maintenanceIpWhitelist: '',
    maintenanceRoleWhitelist: 'Admin',
    emailSenderName: 'Eskineria',
    emailSenderAddress: 'no-reply@eskineria.local',
    emailDailySendLimit: 5000,
    emailRetryMaxAttempts: 3,
    notificationLoginAlertEnabled: true,
    notificationSecurityEmailRecipients: '',
    localizationDefaultCulture: 'en-US',
    localizationFallbackCulture: 'en-US',
    localizationRequireUserCultureSelection: false,
    auditRetentionDays: 365,
    auditCleanupScheduleHourUtc: 2,
    auditPiiMaskingEnabled: true,
    auditLogReadOperationsEnabled: true,
    auditLogCreateOperationsEnabled: true,
    auditLogUpdateOperationsEnabled: true,
    auditLogDeleteOperationsEnabled: true,
    auditLogOtherOperationsEnabled: true,
    auditLogErrorEventsEnabled: true,
    applicationName: 'Eskineria Backend',
    applicationLogoUrl: null,
    applicationFaviconUrl: null,
}

const toDateTimeLocalValue = (iso: string | null | undefined) => {
    if (!iso) return ''
    const date = new Date(iso)
    if (Number.isNaN(date.getTime())) return ''
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
    return local.toISOString().slice(0, 16)
}

const buildCultureOptions = (...cultureGroups: Array<Array<string | null | undefined>>) => (
    Array.from(
        new Set(
            cultureGroups
                .flat()
                .map((culture) => culture?.trim())
                .filter((culture): culture is string => Boolean(culture)),
        ),
    ).sort((a, b) => a.localeCompare(b))
)

const resolveCultureSetting = (value: string | undefined, cultureOptions: string[], fallbackValue?: string) => {
    const normalizedValue = value?.trim()
    if (normalizedValue && cultureOptions.includes(normalizedValue)) {
        return normalizedValue
    }

    const normalizedFallbackValue = fallbackValue?.trim()
    if (normalizedFallbackValue && cultureOptions.includes(normalizedFallbackValue)) {
        return normalizedFallbackValue
    }

    if (normalizedFallbackValue) {
        return normalizedFallbackValue
    }

    if (cultureOptions.length > 0) {
        return cultureOptions[0]
    }

    return cultureOptions[0] ?? DEFAULT_SETTINGS.localizationDefaultCulture ?? 'en-US'
}

const TagInput = ({ value, onChange, placeholder }: { value: string; onChange: (val: string) => void; placeholder?: string }) => {
    const [inputValue, setInputValue] = useState('')
    const tags = value ? value.split(',').map((t) => t.trim()).filter(Boolean) : []

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === ',' || e.key === 'Enter') {
            e.preventDefault()
            const trimmed = inputValue.trim()
            if (trimmed && !tags.includes(trimmed)) {
                onChange(value ? `${value},${trimmed}` : trimmed)
            }
            setInputValue('')
        } else if (e.key === 'Backspace' && !inputValue && tags.length > 0) {
            const newTags = [...tags]
            newTags.pop()
            onChange(newTags.join(','))
        }
    }

    const removeTag = (index: number) => {
        const newTags = tags.filter((_, i) => i !== index)
        onChange(newTags.join(','))
    }

    return (
        <div className="tag-input-wrapper" onClick={() => document.getElementById(`tag-input-${placeholder}`)?.focus()}>
            {tags.map((tag, idx) => (
                <div key={idx} className="tag-item">
                    {tag}
                    <span className="tag-remove" onClick={(e) => { e.stopPropagation(); removeTag(idx); }}>
                        <LuX size={12} />
                    </span>
                </div>
            ))}
            <input
                id={`tag-input-${placeholder}`}
                type="text"
                value={inputValue}
                onChange={(e) => setInputValue(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder={tags.length === 0 ? placeholder : ''}
                autoComplete="off"
            />
        </div>
    )
}

const SystemSettings = () => {
    const { t } = useTranslation()
    const { applyBranding } = useBranding()

    const getAssetUrl = (url: string | null | undefined) => {
        if (!url) return null
        if (url.startsWith('http')) return url
        const baseUrl = import.meta.env.VITE_API_BASE_URL
        return `${baseUrl}${url.startsWith('/') ? '' : '/'}${url}`
    }

    const [activeTab, setActiveTab] = useState<SettingsTab>('branding')
    const [settings, setSettings] = useState<AuthSystemSettings>(DEFAULT_SETTINGS)
    const [isLoading, setIsLoading] = useState(true)
    const [isSaving, setIsSaving] = useState(false)
    const [isUploadingLogo, setIsUploadingLogo] = useState(false)
    const [isUploadingFavicon, setIsUploadingFavicon] = useState(false)
    const [allRoles, setAllRoles] = useState<string[]>([])
    const [availableCultures, setAvailableCultures] = useState<string[]>(() => buildCultureOptions([
        DEFAULT_SETTINGS.localizationDefaultCulture,
        DEFAULT_SETTINGS.localizationFallbackCulture,
    ]))

    const loadSettings = async () => {
        setIsLoading(true)
        try {
            const [settingsRes, rolesRes, culturesRes] = await Promise.all([
                SystemSettingsService.getAuthSettings(),
                AccessControlService.getRoles({ pageNumber: 1, pageSize: 200 }),
                localizationService.getCultures().catch(() => []),
            ])

            const cultureOptions = buildCultureOptions(
                culturesRes,
                [
                    settingsRes.data?.localizationDefaultCulture,
                    settingsRes.data?.localizationFallbackCulture,
                    DEFAULT_SETTINGS.localizationDefaultCulture,
                    DEFAULT_SETTINGS.localizationFallbackCulture,
                ],
            )

            setAvailableCultures(cultureOptions)

            if (settingsRes.success && settingsRes.data) {
                const localizationDefaultCulture = resolveCultureSetting(
                    settingsRes.data.localizationDefaultCulture,
                    cultureOptions,
                    DEFAULT_SETTINGS.localizationDefaultCulture,
                )
                const localizationFallbackCulture = resolveCultureSetting(
                    settingsRes.data.localizationFallbackCulture,
                    cultureOptions,
                    localizationDefaultCulture,
                )

                setSettings({
                    ...DEFAULT_SETTINGS,
                    ...settingsRes.data,
                    localizationDefaultCulture,
                    localizationFallbackCulture,
                    applicationLogoUrl: settingsRes.data.applicationLogoUrl || null,
                    applicationFaviconUrl: settingsRes.data.applicationFaviconUrl || null,
                })
            } else {
                setSettings((prev) => ({
                    ...prev,
                    localizationDefaultCulture: resolveCultureSetting(
                        prev.localizationDefaultCulture,
                        cultureOptions,
                        DEFAULT_SETTINGS.localizationDefaultCulture,
                    ),
                    localizationFallbackCulture: resolveCultureSetting(
                        prev.localizationFallbackCulture,
                        cultureOptions,
                        DEFAULT_SETTINGS.localizationFallbackCulture,
                    ),
                }))
            }

            if (rolesRes.items) {
                setAllRoles(rolesRes.items.map(r => r.name))
            }
        } catch (error) {
            console.error('Failed to load settings:', error)
        } finally {
            setIsLoading(false)
        }
    }

    useEffect(() => {
        void loadSettings()
    }, [])

    const handleSave = async () => {
        setIsSaving(true)
        try {
            const localizationDefaultCulture = resolveCultureSetting(
                settings.localizationDefaultCulture,
                availableCultures,
                DEFAULT_SETTINGS.localizationDefaultCulture,
            )
            const localizationFallbackCulture = resolveCultureSetting(
                settings.localizationFallbackCulture,
                availableCultures,
                localizationDefaultCulture,
            )
            const payload: AuthSystemSettings = {
                ...settings,
                applicationName: settings.applicationName?.trim() || DEFAULT_SETTINGS.applicationName,
                localizationDefaultCulture,
                localizationFallbackCulture,
            }
            const response = await SystemSettingsService.updateAuthSettings(payload)
            if (response.success) {
                setSettings((prev) => ({
                    ...prev,
                    localizationDefaultCulture,
                    localizationFallbackCulture,
                }))
                showToast(response.message || t('identity.system_settings.save_success'), 'success')
                applyBranding({
                    applicationName: payload.applicationName || DEFAULT_SETTINGS.applicationName || 'Eskineria Backend',
                })
            }
        } catch (error: any) {
            // Error is handled by global interceptor, but we can log for debug
            console.error('Save failed:', error);
        } finally {
            setIsSaving(false)
        }
    }

    const handleLogoUpload = async (file: File) => {
        setIsUploadingLogo(true)
        try {
            const response = await SystemSettingsService.uploadApplicationLogo(file)
            if (response.success && response.data?.url) {
                setSettings((prev) => ({
                    ...prev,
                    applicationLogoUrl: response.data?.url || null,
                }))
                applyBranding({
                    applicationLogoUrl: response.data.url,
                })
                showToast(response.message || t('identity.system_settings.logo_upload_success'), 'success')
            }
        } catch {
            // Global axios interceptor handles toast.
        } finally {
            setIsUploadingLogo(false)
        }
    }

    const handleFaviconUpload = async (file: File) => {
        setIsUploadingFavicon(true)
        try {
            const response = await SystemSettingsService.uploadApplicationFavicon(file)
            if (response.success && response.data?.url) {
                setSettings((prev) => ({
                    ...prev,
                    applicationFaviconUrl: response.data?.url || null,
                }))
                applyBranding({
                    applicationFaviconUrl: response.data.url,
                })
                showToast(response.message || t('identity.system_settings.favicon_upload_success'), 'success')
            }
        } catch {
            // Global axios interceptor handles toast.
        } finally {
            setIsUploadingFavicon(false)
        }
    }

    const handleNumberSettingChange = (key: NumberSettingKey, value: string) => {
        const numeric = Number(value)
        const normalized = Number.isFinite(numeric) ? Math.max(0, Math.trunc(numeric)) : 0
        setSettings((prev) => ({ ...prev, [key]: normalized }))
    }

    const handleTextSettingChange = (key: TextSettingKey, value: string) => {
        setSettings((prev) => ({ ...prev, [key]: value }))
    }

    const selectedMaintenanceRoles = (settings.maintenanceRoleWhitelist || '')
        .split(',')
        .map(s => s.trim())
        .filter(s => s.length > 0)

    const maintenanceRoleOptions = Array.from(new Set([...allRoles, ...selectedMaintenanceRoles]))
        .sort((a, b) => a.localeCompare(b))
        .map((role) => ({ label: role, value: role }))

    const selectedMaintenanceRoleOptions = maintenanceRoleOptions.filter((option) =>
        selectedMaintenanceRoles.includes(option.value))

    const handleMaintenanceRolesChange = (options: MultiValue<SelectOption>) => {
        handleTextSettingChange('maintenanceRoleWhitelist', options.map((option) => option.value).join(','))
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('identity.system_settings.title')} subtitle={t('identity.title')} />
            <Container fluid>
                <Row>
                    <Col xs={12}>
                        <Card className="system-settings-shell">
                            <Card.Body>
                                <h5 className="mb-2 system-settings-title">{t('identity.system_settings.title')}</h5>
                                <p className="text-muted mb-4 system-settings-subtitle">
                                    {t('identity.system_settings.subtitle')}
                                </p>

                                {isLoading ? (
                                    <div className="d-flex align-items-center gap-2 text-muted">
                                        <Spinner animation="border" size="sm" />
                                        <span>{t('common.loading')}</span>
                                    </div>
                                ) : (
                                    <Form>
                                        <Tabs
                                            id="system-settings-tabs"
                                            activeKey={activeTab}
                                            onSelect={(key) => setActiveTab((key as SettingsTab) || 'branding')}
                                            className="system-settings-tabs mb-3"
                                            mountOnEnter
                                            unmountOnExit
                                        >
                                            <Tab eventKey="branding" title={t('identity.system_settings.tab_branding')}>
                                                <div className="system-settings-pane">
                                                    <Form.Group className="mb-3">
                                                        <Form.Label htmlFor="setting-application-name">
                                                            {t('identity.system_settings.application_name')}
                                                        </Form.Label>
                                                        <Form.Control
                                                            id="setting-application-name"
                                                            type="text"
                                                            maxLength={120}
                                                            value={settings.applicationName || ''}
                                                            onChange={(event) => handleTextSettingChange('applicationName', event.target.value)}
                                                        />
                                                        <Form.Text className="text-muted fs-xs mt-1">
                                                            {t('identity.system_settings.application_name_desc')}
                                                        </Form.Text>
                                                    </Form.Group>

                                                    <Row className="g-3 branding-row">
                                                        <Col md={6}>
                                                            <Form.Group>
                                                                <Form.Label className="fw-bold mb-2">
                                                                    {t('identity.system_settings.application_logo')}
                                                                </Form.Label>
                                                                <div className="branding-asset-card p-3 border rounded shadow-sm bg-light bg-opacity-10">
                                                                    <div className="branding-asset-preview mb-2 d-flex align-items-center justify-content-center border rounded bg-white" style={{ height: '120px', overflow: 'hidden' }}>
                                                                        {settings.applicationLogoUrl ? (
                                                                            <img
                                                                                src={getAssetUrl(settings.applicationLogoUrl)!}
                                                                                alt={t('identity.system_settings.application_logo_preview_alt')}
                                                                                style={{ maxHeight: '80%', maxWidth: '90%', objectFit: 'contain' }}
                                                                            />
                                                                        ) : (
                                                                            <div className="text-center text-muted p-4">
                                                                                <LuPlus size={24} className="opacity-25 mb-1" />
                                                                                <div className="fs-xs">{t('identity.system_settings.no_logo_uploaded')}</div>
                                                                            </div>
                                                                        )}
                                                                    </div>
                                                                    <Form.Control
                                                                        id="setting-application-logo"
                                                                        className="form-control-sm"
                                                                        type="file"
                                                                        accept=".png,.jpg,.jpeg,image/png,image/jpeg"
                                                                        disabled={isUploadingLogo}
                                                                        onChange={(event) => {
                                                                            const file = (event.target as HTMLInputElement).files?.[0]
                                                                            if (file) void handleLogoUpload(file)
                                                                            event.currentTarget.value = ''
                                                                        }}
                                                                    />
                                                                    <Form.Text className="text-muted fs-xs mt-1 d-block">
                                                                        {t('identity.system_settings.logo_upload_hint')}
                                                                    </Form.Text>
                                                                </div>
                                                            </Form.Group>
                                                        </Col>

                                                        <Col md={6}>
                                                            <Form.Group>
                                                                <Form.Label className="fw-bold mb-2">
                                                                    {t('identity.system_settings.application_favicon')}
                                                                </Form.Label>
                                                                <div className="branding-asset-card p-3 border rounded shadow-sm bg-light bg-opacity-10">
                                                                    <div className="branding-asset-preview mb-2 d-flex align-items-center justify-content-center border rounded bg-white" style={{ height: '120px', overflow: 'hidden' }}>
                                                                        {settings.applicationFaviconUrl ? (
                                                                            <div className="text-center">
                                                                                <img
                                                                                    src={getAssetUrl(settings.applicationFaviconUrl)!}
                                                                                    alt={t('identity.system_settings.application_favicon_preview_alt')}
                                                                                    width={32}
                                                                                    height={32}
                                                                                    className="shadow-sm mb-2"
                                                                                />
                                                                                <div className="text-muted small fs-xs d-block">{t('identity.system_settings.favicon_active')}</div>
                                                                            </div>
                                                                        ) : (
                                                                            <div className="text-center text-muted p-4">
                                                                                <LuPlus size={24} className="opacity-25 mb-1" />
                                                                                <div className="fs-xs">{t('identity.system_settings.no_favicon_uploaded')}</div>
                                                                            </div>
                                                                        )}
                                                                    </div>
                                                                    <Form.Control
                                                                        id="setting-application-favicon"
                                                                        className="form-control-sm"
                                                                        type="file"
                                                                        accept=".png,.ico,image/png,image/x-icon,image/vnd.microsoft.icon"
                                                                        disabled={isUploadingFavicon}
                                                                        onChange={(event) => {
                                                                            const file = (event.target as HTMLInputElement).files?.[0]
                                                                            if (file) void handleFaviconUpload(file)
                                                                            event.currentTarget.value = ''
                                                                        }}
                                                                    />
                                                                    <Form.Text className="text-muted fs-xs mt-1 d-block">
                                                                        {t('identity.system_settings.favicon_upload_hint')}
                                                                    </Form.Text>
                                                                </div>
                                                            </Form.Group>
                                                        </Col>
                                                    </Row>
                                                </div>
                                            </Tab>

                                            <Tab eventKey="access" title={t('identity.system_settings.tab_access')}>
                                                <div className="system-settings-pane">
                                                    <Row className="g-3 mb-3">
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-login-enabled"
                                                                label={t('identity.system_settings.login_enabled')}
                                                                checked={settings.loginEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, loginEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-register-enabled"
                                                                label={t('identity.system_settings.register_enabled')}
                                                                checked={settings.registerEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, registerEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-google-login-enabled"
                                                                label={t('identity.system_settings.google_login_enabled')}
                                                                checked={settings.googleLoginEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, googleLoginEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-forgot-password-enabled"
                                                                label={t('identity.system_settings.forgot_password_enabled')}
                                                                checked={settings.forgotPasswordEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, forgotPasswordEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-change-password-enabled"
                                                                label={t('identity.system_settings.change_password_enabled')}
                                                                checked={settings.changePasswordEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, changePasswordEnabled: event.target.checked }))}
                                                        />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-session-management-enabled"
                                                                label={t('identity.system_settings.session_management_enabled')}
                                                                checked={settings.sessionManagementEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, sessionManagementEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                    </Row>

                                                    <hr />

                                                    <Row className="g-3 mb-3 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-email-verification-required"
                                                                label={t('identity.system_settings.email_verification_required')}
                                                                checked={settings.emailVerificationRequired}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, emailVerificationRequired: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-email-verification-code-expiry-seconds">
                                                                {t('identity.system_settings.email_verification_code_expiry_seconds')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-email-verification-code-expiry-seconds"
                                                                type="number"
                                                                min={30}
                                                                step={1}
                                                                value={settings.emailVerificationCodeExpirySeconds}
                                                                onChange={(event) => handleNumberSettingChange('emailVerificationCodeExpirySeconds', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-email-verification-resend-cooldown-seconds">
                                                                {t('identity.system_settings.email_verification_resend_cooldown_seconds')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-email-verification-resend-cooldown-seconds"
                                                                type="number"
                                                                min={5}
                                                                step={1}
                                                                value={settings.emailVerificationResendCooldownSeconds}
                                                                onChange={(event) => handleNumberSettingChange('emailVerificationResendCooldownSeconds', event.target.value)}
                                                            />
                                                        </Col>
                                                    </Row>

                                                    <hr />

                                                    <Row className="g-3 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-registration-invitation-required"
                                                                label={t('identity.system_settings.registration_invitation_required')}
                                                                checked={settings.registrationInvitationRequired ?? DEFAULT_SETTINGS.registrationInvitationRequired}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, registrationInvitationRequired: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-registration-auto-approve-enabled"
                                                                label={t('identity.system_settings.registration_auto_approve_enabled')}
                                                                checked={settings.registrationAutoApproveEnabled ?? DEFAULT_SETTINGS.registrationAutoApproveEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, registrationAutoApproveEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={6}>
                                                            <Form.Label htmlFor="setting-registration-allowed-email-domains">
                                                                {t('identity.system_settings.registration_allowed_email_domains')}
                                                            </Form.Label>
                                                            <TagInput
                                                                placeholder="eskineria.com, company.local"
                                                                value={settings.registrationAllowedEmailDomains ?? ''}
                                                                onChange={(val) => handleTextSettingChange('registrationAllowedEmailDomains', val)}
                                                            />
                                                        </Col>
                                                        <Col md={6}>
                                                            <Form.Label htmlFor="setting-registration-blocked-email-domains">
                                                                {t('identity.system_settings.registration_blocked_email_domains')}
                                                            </Form.Label>
                                                            <TagInput
                                                                placeholder="gmail.com, hotmail.com"
                                                                value={settings.registrationBlockedEmailDomains ?? ''}
                                                                onChange={(val) => handleTextSettingChange('registrationBlockedEmailDomains', val)}
                                                            />
                                                        </Col>
                                                    </Row>
                                                </div>
                                            </Tab>

                                            <Tab eventKey="session" title={t('identity.system_settings.tab_session')}>
                                                <div className="system-settings-pane">
                                                    <Row className="g-3 mb-3 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-session-access-token-lifetime-minutes">
                                                                {t('identity.system_settings.session_access_token_lifetime_minutes')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-session-access-token-lifetime-minutes"
                                                                type="number"
                                                                min={5}
                                                                step={1}
                                                                value={settings.sessionAccessTokenLifetimeMinutes}
                                                                onChange={(event) => handleNumberSettingChange('sessionAccessTokenLifetimeMinutes', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-session-refresh-token-lifetime-days">
                                                                {t('identity.system_settings.session_refresh_token_lifetime_days')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-session-refresh-token-lifetime-days"
                                                                type="number"
                                                                min={1}
                                                                step={1}
                                                                value={settings.sessionRefreshTokenLifetimeDays}
                                                                onChange={(event) => handleNumberSettingChange('sessionRefreshTokenLifetimeDays', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-session-max-active-sessions">
                                                                {t('identity.system_settings.session_max_active_sessions')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-session-max-active-sessions"
                                                                type="number"
                                                                min={1}
                                                                step={1}
                                                                value={settings.sessionMaxActiveSessions}
                                                                onChange={(event) => handleNumberSettingChange('sessionMaxActiveSessions', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-session-idle-timeout-minutes">
                                                                {t('identity.system_settings.session_idle_timeout_minutes')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-session-idle-timeout-minutes"
                                                                type="number"
                                                                min={0}
                                                                step={1}
                                                                value={settings.sessionIdleTimeoutMinutes}
                                                                onChange={(event) => handleNumberSettingChange('sessionIdleTimeoutMinutes', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-session-warning-before-timeout-minutes">
                                                                {t('identity.system_settings.session_warning_before_timeout_minutes')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-session-warning-before-timeout-minutes"
                                                                type="number"
                                                                min={0}
                                                                step={1}
                                                                value={settings.sessionWarningBeforeTimeoutMinutes}
                                                                onChange={(event) => handleNumberSettingChange('sessionWarningBeforeTimeoutMinutes', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-session-remember-me-duration-days">
                                                                {t('identity.system_settings.session_remember_me_duration_days')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-session-remember-me-duration-days"
                                                                type="number"
                                                                min={1}
                                                                step={1}
                                                                value={settings.sessionRememberMeDurationDays}
                                                                onChange={(event) => handleNumberSettingChange('sessionRememberMeDurationDays', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={12}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-session-single-device-mode-enabled"
                                                                label={t('identity.system_settings.session_single_device_mode_enabled')}
                                                                checked={settings.sessionSingleDeviceModeEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, sessionSingleDeviceModeEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                    </Row>
                                                </div>
                                            </Tab>

                                            <Tab eventKey="security" title={t('identity.system_settings.tab_security')}>
                                                <div className="system-settings-pane">
                                                    <Row className="g-3 mb-3 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-password-min-length">
                                                                {t('identity.system_settings.password_min_length')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-password-min-length"
                                                                type="number"
                                                                min={6}
                                                                step={1}
                                                                value={settings.passwordMinLength}
                                                                onChange={(event) => handleNumberSettingChange('passwordMinLength', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-password-require-uppercase"
                                                                label={t('identity.system_settings.password_require_uppercase')}
                                                                checked={settings.passwordRequireUppercase}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, passwordRequireUppercase: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-password-require-lowercase"
                                                                label={t('identity.system_settings.password_require_lowercase')}
                                                                checked={settings.passwordRequireLowercase}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, passwordRequireLowercase: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-password-require-digit"
                                                                label={t('identity.system_settings.password_require_digit')}
                                                                checked={settings.passwordRequireDigit}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, passwordRequireDigit: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-password-require-non-alphanumeric"
                                                                label={t('identity.system_settings.password_require_non_alphanumeric')}
                                                                checked={settings.passwordRequireNonAlphanumeric}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, passwordRequireNonAlphanumeric: event.target.checked }))}
                                                            />
                                                        </Col>
                                                    </Row>

                                                    <hr />

                                                    <Row className="g-3 mb-3 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-login-lockout-enabled"
                                                                label={t('identity.system_settings.login_lockout_enabled')}
                                                                checked={settings.loginLockoutEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, loginLockoutEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        {settings.loginLockoutEnabled && (
                                                            <>
                                                                <Col md={4}>
                                                                    <Form.Label htmlFor="setting-login-max-failed-attempts">
                                                                        {t('identity.system_settings.login_max_failed_attempts')}
                                                                    </Form.Label>
                                                                    <Form.Control
                                                                        id="setting-login-max-failed-attempts"
                                                                        type="number"
                                                                        min={3}
                                                                        step={1}
                                                                        value={settings.loginMaxFailedAttempts}
                                                                        onChange={(event) => handleNumberSettingChange('loginMaxFailedAttempts', event.target.value)}
                                                                    />
                                                                </Col>
                                                                <Col md={4}>
                                                                    <Form.Label htmlFor="setting-login-lockout-duration-minutes">
                                                                        {t('identity.system_settings.login_lockout_duration_minutes')}
                                                                    </Form.Label>
                                                                    <Form.Control
                                                                        id="setting-login-lockout-duration-minutes"
                                                                        type="number"
                                                                        min={1}
                                                                        step={1}
                                                                        value={settings.loginLockoutDurationMinutes}
                                                                        onChange={(event) => handleNumberSettingChange('loginLockoutDurationMinutes', event.target.value)}
                                                                    />
                                                                </Col>
                                                            </>
                                                        )}
                                                    </Row>

                                                    <hr />

                                                    <Row className="g-3 mb-3 settings-mixed-row">
                                                        <Col md={12}>
                                                            <div className="d-flex flex-column gap-2 mt-2">
                                                                <Form.Check
                                                                    type="switch"
                                                                    id="setting-mfa-feature-enabled"
                                                                    label={t('identity.system_settings.mfa_feature_enabled')}
                                                                    checked={settings.mfaFeatureEnabled ?? true}
                                                                    onChange={(event) => setSettings((prev) => ({ ...prev, mfaFeatureEnabled: event.target.checked }))}
                                                                />
                                                                <Form.Check
                                                                    type="switch"
                                                                    id="setting-mfa-enforced-for-all"
                                                                    label={t('identity.system_settings.mfa_enforced_for_all')}
                                                                    checked={settings.mfaEnforcedForAll}
                                                                    onChange={(event) => setSettings((prev) => ({ ...prev, mfaEnforcedForAll: event.target.checked }))}
                                                                />
                                                            </div>
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-mfa-trusted-device-duration-days">
                                                                {t('identity.system_settings.mfa_trusted_device_duration_days')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-mfa-trusted-device-duration-days"
                                                                type="number"
                                                                min={0}
                                                                step={1}
                                                                value={settings.mfaTrustedDeviceDurationDays}
                                                                onChange={(event) => handleNumberSettingChange('mfaTrustedDeviceDurationDays', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={12}>
                                                            <Form.Label htmlFor="setting-mfa-bypass-ip-whitelist">
                                                                {t('identity.system_settings.mfa_bypass_ip_whitelist')}
                                                            </Form.Label>
                                                            <TagInput
                                                                placeholder={t('identity.system_settings.mfa_bypass_ip_whitelist_placeholder')}
                                                                value={settings.mfaBypassIpWhitelist ?? ''}
                                                                onChange={(val) => handleTextSettingChange('mfaBypassIpWhitelist', val)}
                                                            />
                                                        </Col>
                                                    </Row>

                                                    <hr />

                                                    <Row className="g-3 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-account-inactive-lock-days">
                                                                {t('identity.system_settings.account_inactive_lock_days')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-account-inactive-lock-days"
                                                                type="number"
                                                                min={7}
                                                                step={1}
                                                                value={settings.accountInactiveLockDays}
                                                                onChange={(event) => handleNumberSettingChange('accountInactiveLockDays', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-account-password-expiry-days">
                                                                {t('identity.system_settings.account_password_expiry_days')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-account-password-expiry-days"
                                                                type="number"
                                                                min={0}
                                                                step={1}
                                                                value={settings.accountPasswordExpiryDays}
                                                                onChange={(event) => handleNumberSettingChange('accountPasswordExpiryDays', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-account-force-password-change-on-first-login"
                                                                label={t('identity.system_settings.account_force_password_change_on_first_login')}
                                                                checked={settings.accountForcePasswordChangeOnFirstLogin}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, accountForcePasswordChangeOnFirstLogin: event.target.checked }))}
                                                            />
                                                        </Col>
                                                    </Row>
                                                </div>
                                            </Tab>

                                            <Tab eventKey="maintenance" title={t('identity.system_settings.tab_maintenance')}>
                                                <div className="system-settings-pane">
                                                    <Row className="g-3 mb-3 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-maintenance-mode-enabled"
                                                                label={t('identity.system_settings.maintenance_mode_enabled')}
                                                                checked={settings.maintenanceModeEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, maintenanceModeEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-maintenance-countdown-enabled"
                                                                label={t('identity.system_settings.maintenance_countdown_enabled')}
                                                                checked={settings.maintenanceCountdownEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, maintenanceCountdownEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-maintenance-end-time">
                                                                {t('identity.system_settings.maintenance_end_time')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-maintenance-end-time"
                                                                type="datetime-local"
                                                                value={toDateTimeLocalValue(settings.maintenanceEndTime)}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, maintenanceEndTime: event.target.value ? new Date(event.target.value).toISOString() : null }))}
                                                            />
                                                        </Col>
                                                        <Col md={12}>
                                                            <Form.Label htmlFor="setting-maintenance-message">
                                                                {t('identity.system_settings.maintenance_message')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-maintenance-message"
                                                                as="textarea"
                                                                rows={2}
                                                                value={settings.maintenanceMessage ?? ''}
                                                                onChange={(event) => handleTextSettingChange('maintenanceMessage', event.target.value)}
                                                            />
                                                        </Col>
                                                    </Row>

                                                    <Row className="g-3 settings-top-aligned-row">
                                                        <Col md={6} className="settings-labeled-field">
                                                            <Form.Label htmlFor="setting-maintenance-ip-whitelist">
                                                                {t('identity.system_settings.maintenance_ip_whitelist')}
                                                            </Form.Label>
                                                            <TagInput
                                                                placeholder="192.168.1.1, 10.0.0.0/24"
                                                                value={settings.maintenanceIpWhitelist ?? ''}
                                                                onChange={(val) => handleTextSettingChange('maintenanceIpWhitelist', val)}
                                                            />
                                                        </Col>
                                                        <Col md={6} className="settings-labeled-field">
                                                            <Form.Label htmlFor="setting-maintenance-role-whitelist">
                                                                {t('identity.system_settings.maintenance_role_whitelist')}
                                                            </Form.Label>
                                                            <Select<SelectOption, true>
                                                                inputId="setting-maintenance-role-whitelist"
                                                                isMulti
                                                                isSearchable
                                                                closeMenuOnSelect={false}
                                                                hideSelectedOptions={false}
                                                                className="react-select maintenance-role-select"
                                                                classNamePrefix="react-select"
                                                                options={maintenanceRoleOptions}
                                                                value={selectedMaintenanceRoleOptions}
                                                                onChange={handleMaintenanceRolesChange}
                                                                placeholder={t('common.search')}
                                                                noOptionsMessage={() => t('identity.no_roles_found')}
                                                            />
                                                            <Form.Text className="text-muted fs-xs mt-1">
                                                                {t('identity.system_settings.maintenance_role_whitelist_desc')}
                                                            </Form.Text>
                                                        </Col>
                                                    </Row>
                                                </div>
                                            </Tab>

                                            <Tab eventKey="communication" title={t('identity.system_settings.tab_communication')}>
                                                <div className="system-settings-pane">
                                                    <Row className="g-3 mb-3 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-email-sender-name">
                                                                {t('identity.system_settings.email_sender_name')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-email-sender-name"
                                                                value={settings.emailSenderName ?? ''}
                                                                onChange={(event) => handleTextSettingChange('emailSenderName', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-email-sender-address">
                                                                {t('identity.system_settings.email_sender_address')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-email-sender-address"
                                                                type="email"
                                                                value={settings.emailSenderAddress ?? ''}
                                                                onChange={(event) => handleTextSettingChange('emailSenderAddress', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={2}>
                                                            <Form.Label htmlFor="setting-email-daily-send-limit">
                                                                {t('identity.system_settings.email_daily_send_limit')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-email-daily-send-limit"
                                                                type="number"
                                                                min={0}
                                                                step={1}
                                                                value={settings.emailDailySendLimit}
                                                                onChange={(event) => handleNumberSettingChange('emailDailySendLimit', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={2}>
                                                            <Form.Label htmlFor="setting-email-retry-max-attempts">
                                                                {t('identity.system_settings.email_retry_max_attempts')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-email-retry-max-attempts"
                                                                type="number"
                                                                min={0}
                                                                step={1}
                                                                value={settings.emailRetryMaxAttempts}
                                                                onChange={(event) => handleNumberSettingChange('emailRetryMaxAttempts', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-notification-login-alert-enabled"
                                                                label={t('identity.system_settings.notification_login_alert_enabled')}
                                                                checked={settings.notificationLoginAlertEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, notificationLoginAlertEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={12}>
                                                            <Form.Label htmlFor="setting-notification-security-email-recipients">
                                                                {t('identity.system_settings.notification_security_email_recipients')}
                                                            </Form.Label>
                                                            <TagInput
                                                                placeholder="admin@eskineria.com, security@eskineria.com"
                                                                value={settings.notificationSecurityEmailRecipients ?? ''}
                                                                onChange={(val) => handleTextSettingChange('notificationSecurityEmailRecipients', val)}
                                                            />
                                                        </Col>
                                                    </Row>
                                                </div>
                                            </Tab>

                                            <Tab eventKey="localization" title={t('identity.system_settings.tab_localization')}>
                                                <div className="system-settings-pane">
                                                    <Row className="g-3 mb-3 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-localization-default-culture">
                                                                {t('identity.system_settings.localization_default_culture')}
                                                            </Form.Label>
                                                            <Form.Select
                                                                id="setting-localization-default-culture"
                                                                value={resolveCultureSetting(
                                                                    settings.localizationDefaultCulture,
                                                                    availableCultures,
                                                                    DEFAULT_SETTINGS.localizationDefaultCulture,
                                                                )}
                                                                onChange={(event) => handleTextSettingChange('localizationDefaultCulture', event.target.value)}
                                                            >
                                                                {availableCultures.map((culture) => (
                                                                    <option key={`default-culture-${culture}`} value={culture}>
                                                                        {culture}
                                                                    </option>
                                                                ))}
                                                            </Form.Select>
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-localization-fallback-culture">
                                                                {t('identity.system_settings.localization_fallback_culture')}
                                                            </Form.Label>
                                                            <Form.Select
                                                                id="setting-localization-fallback-culture"
                                                                value={resolveCultureSetting(
                                                                    settings.localizationFallbackCulture,
                                                                    availableCultures,
                                                                    resolveCultureSetting(
                                                                        settings.localizationDefaultCulture,
                                                                        availableCultures,
                                                                        DEFAULT_SETTINGS.localizationDefaultCulture,
                                                                    ),
                                                                )}
                                                                onChange={(event) => handleTextSettingChange('localizationFallbackCulture', event.target.value)}
                                                            >
                                                                {availableCultures.map((culture) => (
                                                                    <option key={`fallback-culture-${culture}`} value={culture}>
                                                                        {culture}
                                                                    </option>
                                                                ))}
                                                            </Form.Select>
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-localization-require-user-culture-selection"
                                                                label={t('identity.system_settings.localization_require_user_culture_selection')}
                                                                checked={settings.localizationRequireUserCultureSelection}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, localizationRequireUserCultureSelection: event.target.checked }))}
                                                            />
                                                        </Col>
                                                    </Row>
                                                </div>
                                            </Tab>

                                            <Tab eventKey="audit" title={t('identity.system_settings.tab_audit')}>
                                                <div className="system-settings-pane">
                                                    <Row className="g-3 mb-4 settings-mixed-row">
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-audit-retention-days">
                                                                {t('identity.system_settings.audit_retention_days')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-audit-retention-days"
                                                                type="number"
                                                                min={30}
                                                                step={1}
                                                                value={settings.auditRetentionDays}
                                                                onChange={(event) => handleNumberSettingChange('auditRetentionDays', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Label htmlFor="setting-audit-cleanup-schedule-hour-utc">
                                                                {t('identity.system_settings.audit_cleanup_schedule_hour_utc')}
                                                            </Form.Label>
                                                            <Form.Control
                                                                id="setting-audit-cleanup-schedule-hour-utc"
                                                                type="number"
                                                                min={0}
                                                                max={23}
                                                                step={1}
                                                                value={settings.auditCleanupScheduleHourUtc}
                                                                onChange={(event) => handleNumberSettingChange('auditCleanupScheduleHourUtc', event.target.value)}
                                                            />
                                                        </Col>
                                                        <Col md={4} className="d-flex align-items-end">
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-audit-pii-masking-enabled"
                                                                label={t('identity.system_settings.audit_pii_masking_enabled')}
                                                                checked={settings.auditPiiMaskingEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, auditPiiMaskingEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                    </Row>

                                                    <h6 className="mb-3 border-bottom pb-2 text-muted text-uppercase fs-xs fw-bold">
                                                        {t('identity.system_settings.audit_capture_filters_title')}
                                                    </h6>
                                                    <Row className="g-3">
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-audit-log-read"
                                                                label={t('identity.system_settings.audit_log_read_operations_enabled')}
                                                                checked={settings.auditLogReadOperationsEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, auditLogReadOperationsEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-audit-log-create"
                                                                label={t('identity.system_settings.audit_log_create_operations_enabled')}
                                                                checked={settings.auditLogCreateOperationsEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, auditLogCreateOperationsEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-audit-log-update"
                                                                label={t('identity.system_settings.audit_log_update_operations_enabled')}
                                                                checked={settings.auditLogUpdateOperationsEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, auditLogUpdateOperationsEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-audit-log-delete"
                                                                label={t('identity.system_settings.audit_log_delete_operations_enabled')}
                                                                checked={settings.auditLogDeleteOperationsEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, auditLogDeleteOperationsEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-audit-log-other"
                                                                label={t('identity.system_settings.audit_log_other_operations_enabled')}
                                                                checked={settings.auditLogOtherOperationsEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, auditLogOtherOperationsEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                        <Col md={4}>
                                                            <Form.Check
                                                                type="switch"
                                                                id="setting-audit-log-error"
                                                                label={t('identity.system_settings.audit_log_error_events_enabled')}
                                                                checked={settings.auditLogErrorEventsEnabled}
                                                                onChange={(event) => setSettings((prev) => ({ ...prev, auditLogErrorEventsEnabled: event.target.checked }))}
                                                            />
                                                        </Col>
                                                    </Row>
                                                </div>
                                            </Tab>
                                        </Tabs>

                                        <div className="d-flex justify-content-end settings-footer">
                                            <Button onClick={handleSave} disabled={isSaving || isUploadingLogo || isUploadingFavicon}>
                                                {isSaving ? t('common.loading') : t('common.save')}
                                            </Button>
                                        </div>
                                    </Form>
                                )}
                            </Card.Body>
                        </Card>
                    </Col>
                </Row>
            </Container>
        </VerticalLayout>
    )
}

export default SystemSettings
