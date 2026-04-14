import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'

interface ProtectedRouteProps {
    children: ReactNode
    permission?: string
}

const ProtectedRoute = ({ children, permission }: ProtectedRouteProps) => {
    const { permissions, userInfo, isLoading } = useAuth()

    if (isLoading) {
        return (
            <div id="preloader">
                <div className="status">
                    <div className="spinner-border avatar-sm text-primary" role="status"></div>
                </div>
            </div>
        )
    }

    if (!userInfo) {
        return <Navigate to="/auth/login" replace />
    }

    if (permission && !permissions.includes(permission)) {
        return <Navigate to="/error/401" replace />
    }

    return <>{children}</>
}

export default ProtectedRoute
