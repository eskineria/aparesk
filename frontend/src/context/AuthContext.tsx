import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useLocation } from 'react-router-dom'
import { AuthService } from '@/services/authService'
import { ComplianceService } from '@/services/complianceService'
import { showToast } from '@/utils/toast'
import type { UserInfo } from '@/types/auth'
import type { TermsDto } from '@/types/compliance'
import i18n from '@/i18n'

const PUBLIC_PATHS = ['/auth/login', '/auth/register', '/auth/forgot-password', '/reset-password', '/confirm-email', '/error']
const AUTH_SESSION_MARKER_KEY = 'eskineria.authenticated-session'

type AuthContextValue = {
    userInfo: UserInfo | null
    permissions: string[]
    roles: string[]
    activeRole: string | null
    pendingRequiredTerms: TermsDto[]
    isLoading: boolean
    refresh: (force?: boolean) => Promise<boolean>
    switchRole: (roleName: string) => Promise<boolean>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

type AuthProviderProps = { children: ReactNode }

export const AuthProvider = ({ children }: AuthProviderProps) => {
    const location = useLocation()
    const [userInfo, setUserInfo] = useState<UserInfo | null>(null)
    const [permissions, setPermissions] = useState<string[]>([])
    const [pendingRequiredTerms, setPendingRequiredTerms] = useState<TermsDto[]>([])
    const [isLoading, setIsLoading] = useState(true)

    const shouldSkip = PUBLIC_PATHS.some(path => location.pathname.includes(path))

    const refresh = useCallback(async (force?: boolean) => {
        setIsLoading(true)
        if (shouldSkip && !force) {
            setPendingRequiredTerms([])
            setIsLoading(false)
            return false
        }

        try {
            const userResponse = await AuthService.getUserInfo()
            if (userResponse.success && userResponse.data) {
                setUserInfo(userResponse.data)
                sessionStorage.setItem(AUTH_SESSION_MARKER_KEY, '1')

                const [permResponse, pendingTermsResponse] = await Promise.allSettled([
                    AuthService.getPermissions(),
                    ComplianceService.getPendingRequiredTerms(),
                ])

                if (permResponse.status === 'fulfilled' && permResponse.value.success && permResponse.value.data) {
                    setPermissions(permResponse.value.data)
                } else {
                    setPermissions([])
                }

                if (pendingTermsResponse.status === 'fulfilled' && pendingTermsResponse.value.success && pendingTermsResponse.value.data) {
                    setPendingRequiredTerms(pendingTermsResponse.value.data)
                } else {
                    setPendingRequiredTerms([])
                }
                return true
            } else {
                setUserInfo(null)
                setPermissions([])
                setPendingRequiredTerms([])
                sessionStorage.removeItem(AUTH_SESSION_MARKER_KEY)
            }
        } catch {
            // Silent: handled by axios interceptor when needed
            sessionStorage.removeItem(AUTH_SESSION_MARKER_KEY)
            return false
        } finally {
            setIsLoading(false)
        }

        return false
    }, [shouldSkip])

    const switchRole = useCallback(async (roleName: string) => {
        if (!roleName) return false

        try {
            const response = await AuthService.switchRole({ roleName })
            if (response.success && response.data) {
                setUserInfo((prev) => prev ? {
                    ...prev,
                    activeRole: response.data!.activeRole,
                    roles: response.data!.roles,
                } : prev)

                // Defer success toast until dashboard render after redirect
                sessionStorage.setItem('roleSwitchSuccessToast', response.message || i18n.t('auth.role_switched_successfully'))
                return true
            } else {
                showToast(response.message || i18n.t('auth.role_switch_failed'), 'error')

                return false
            }
        } catch (error: unknown) {
            const errorMessage = error instanceof Error && error.message
                ? error.message
                : i18n.t('auth.role_switch_failed')
            showToast(errorMessage, 'error')
            return false
        }
    }, [])

    useEffect(() => {
        refresh()

        const handleProfileUpdate = () => {
            refresh()
        }

        window.addEventListener('user-profile-updated', handleProfileUpdate)

        return () => {
            window.removeEventListener('user-profile-updated', handleProfileUpdate)
        }
    }, [refresh])

    const value = useMemo<AuthContextValue>(() => ({
        userInfo,
        permissions,
        roles: userInfo?.roles || [],
        activeRole: userInfo?.activeRole || null,
        pendingRequiredTerms,
        isLoading,
        refresh,
        switchRole,
    }), [userInfo, permissions, pendingRequiredTerms, isLoading, refresh, switchRole])

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    )
}

export const useAuth = (): AuthContextValue => {
    const context = useContext(AuthContext)
    if (!context) {
        throw new Error('useAuth must be used within AuthProvider')
    }
    return context
}
