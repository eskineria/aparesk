import { Container, Row, Col, Spinner } from 'react-bootstrap'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import { useTranslation } from 'react-i18next'
import { useState, useEffect, useCallback } from 'react'
import { AuthService } from '@/services/authService'
import SystemSettingsService from '@/services/systemSettingsService'
import type { UserInfo, UserSession, UpdateUserInfoRequest, ChangePasswordRequest, MfaStatus, UpdateMfaRequest } from '@/types/auth'
import type { AuthSystemSettings } from '@/types/systemSettings'
import { useForm } from 'react-hook-form'
import { showToast } from '@/utils/toast'
import ProfileInfo from './components/ProfileInfo'
import Account from './components/Account'

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

const Profile = () => {
    const { t } = useTranslation()
    const [userInfo, setUserInfo] = useState<UserInfo | null>(null)
    const [sessions, setSessions] = useState<UserSession[]>([])
    const [mfaStatus, setMfaStatus] = useState<MfaStatus | null>(null)
    const [authSettings, setAuthSettings] = useState<AuthSystemSettings>(DEFAULT_AUTH_SETTINGS)
    const [loading, setLoading] = useState(true)
    const [sessionsLoading, setSessionsLoading] = useState(false)
    const [revokingSessionId, setRevokingSessionId] = useState<string | null>(null)
    const [isRevokingOthers, setIsRevokingOthers] = useState(false)
    const [isUpdatingMfa, setIsUpdatingMfa] = useState(false)

    const { register: profileReg, handleSubmit: handleProfileSubmit, reset: resetProfile, formState: { isSubmitting: isProfileUpdating } } = useForm<UpdateUserInfoRequest>()
    const { register: passwordReg, handleSubmit: handlePasswordSubmit, reset: resetPassword, formState: { isSubmitting: isPasswordUpdating } } = useForm<ChangePasswordRequest>()

    const fetchAuthSettings = useCallback(async () => {
        try {
            const response = await SystemSettingsService.getPublicAuthSettings()
            if (response.success && response.data) {
                setAuthSettings(response.data)
            }
        } catch {
            // Global axios interceptor handles errors.
        }
    }, [])

    const fetchSessions = useCallback(async () => {
        setSessionsLoading(true)
        try {
            const response = await AuthService.getSessions()
            if (response.success && response.data) {
                setSessions(response.data)
            } else {
                setSessions([])
            }
        } catch (error) {
            console.error('Failed to fetch sessions', error)
            setSessions([])
        } finally {
            setSessionsLoading(false)
        }
    }, [])

    const fetchMfaStatus = useCallback(async () => {
        try {
            const response = await AuthService.getMfaStatus()
            if (response.success && response.data) {
                setMfaStatus(response.data)
            } else {
                setMfaStatus(null)
            }
        } catch (error) {
            console.error('Failed to fetch mfa status', error)
            setMfaStatus(null)
        }
    }, [])

    useEffect(() => {
        const fetchUserInfo = async () => {
            try {
                const response = await AuthService.getUserInfo()
                if (response.success && response.data) {
                    setUserInfo(response.data)
                    resetProfile({
                        firstName: response.data.firstName,
                        lastName: response.data.lastName,
                        email: response.data.email,
                        profilePicture: response.data.profilePicture
                    })
                }
            } catch (error) {
                console.error('Failed to fetch user info', error)
            }
        }

        Promise.all([fetchUserInfo(), fetchSessions(), fetchAuthSettings(), fetchMfaStatus()]).finally(() => setLoading(false))
    }, [fetchAuthSettings, fetchMfaStatus, fetchSessions, resetProfile])

    const saveProfile = async (data: UpdateUserInfoRequest) => {
        try {
            const response = await AuthService.updateUserInfo(data)
            if (response.success) {
                showToast(t('profile.update_success'), 'success')
                if (userInfo) {
                    setUserInfo({ ...userInfo, ...data })
                }
                window.dispatchEvent(new Event('user-profile-updated'))
            }
        } catch (error) {
            // Error handled by global interceptor
        }
    }

    const onProfileSubmit = (data: UpdateUserInfoRequest) => {
        saveProfile(data)
    }

    const onPasswordSubmit = async (data: ChangePasswordRequest) => {
        try {
            const response = await AuthService.changePassword(data)
            if (response.success) {
                showToast(t('profile.password_success'), 'success')
                resetPassword()
            }
        } catch (error) {
            // Error handled by global interceptor
        }
    }

    const handleRevokeSession = async (sessionId: string) => {
        const targetSession = sessions.find(x => x.id === sessionId)
        setRevokingSessionId(sessionId)
        try {
            const response = await AuthService.revokeSession(sessionId)
            if (response.success) {
                showToast(t('profile.session_revoke_success'), 'success')
                if (targetSession?.isCurrent) {
                    window.location.href = '/auth/login'
                    return
                }
                await fetchSessions()
            }
        } catch (error) {
            // Error handled by global interceptor
        } finally {
            setRevokingSessionId(null)
        }
    }

    const handleRevokeOtherSessions = async () => {
        setIsRevokingOthers(true)
        try {
            const response = await AuthService.revokeOtherSessions()
            if (response.success) {
                showToast(t('profile.other_sessions_revoked_success'), 'success')
                await fetchSessions()
            }
        } catch (error) {
            // Error handled by global interceptor
        } finally {
            setIsRevokingOthers(false)
        }
    }

    const handleUpdateMfa = async (data: UpdateMfaRequest) => {
        setIsUpdatingMfa(true)
        try {
            const response = await AuthService.updateMfa(data)
            if (response.success && response.data) {
                setMfaStatus(response.data)
                showToast(
                    data.enabled ? t('profile.mfa.enable_success') : t('profile.mfa.disable_success'),
                    'success'
                )
                return true
            }
        } catch {
            // Error handled by global interceptor
            return false
        } finally {
            setIsUpdatingMfa(false)
        }

        return false
    }

    const handleSendMfaCode = async (targetState: boolean) => {
        try {
            const response = await AuthService.sendMfaCode({ targetState })
            if (response.success) {
                showToast(t('profile.mfa.code_sent'), 'success')
                return true
            }
        } catch {
            return false
        }
        return false
    }

    if (loading) {
        return (
            <VerticalLayout>
                <div className="d-flex justify-content-center align-items-center min-vh-100">
                    <Spinner animation="border" variant="primary" />
                </div>
            </VerticalLayout>
        )
    }

    return (
        <VerticalLayout>
            <Container fluid>
                <PageBreadcrumb title={t('profile.title')} />
                <div className="px-3 position-relative">
                    <Row>
                        <Col xl={4}>
                            <ProfileInfo userInfo={userInfo} />
                        </Col>
                        <Col xl={8}>
                            <Account
                                profileReg={profileReg}
                                passwordReg={passwordReg}
                                handleProfileSubmit={handleProfileSubmit}
                                handlePasswordSubmit={handlePasswordSubmit}
                                onProfileSubmit={onProfileSubmit}
                                onPasswordSubmit={onPasswordSubmit}
                                isProfileUpdating={isProfileUpdating}
                                isPasswordUpdating={isPasswordUpdating}
                                sessions={sessions}
                                sessionsLoading={sessionsLoading}
                                revokingSessionId={revokingSessionId}
                                isRevokingOthers={isRevokingOthers}
                                changePasswordEnabled={authSettings.changePasswordEnabled}
                                sessionManagementEnabled={authSettings.sessionManagementEnabled}
                                mfaStatus={mfaStatus}
                                isUpdatingMfa={isUpdatingMfa}
                                hasPassword={userInfo?.hasPassword ?? true}
                                onUpdateMfa={handleUpdateMfa}
                                onSendMfaCode={handleSendMfaCode}
                                onRevokeSession={handleRevokeSession}
                                onRevokeOtherSessions={handleRevokeOtherSessions}
                                t={t}
                            />
                        </Col>
                    </Row>
                </div>
            </Container>
        </VerticalLayout>
    )
}

export default Profile
