import { Routes, Route, Navigate } from 'react-router-dom'
import { ToastContainer } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'

import Login from '@/pages/auth/Login'
import Register from '@/pages/auth/Register'
import ForgotPassword from '@/pages/auth/ForgotPassword'
import ConfirmMail from '@/pages/auth/ConfirmMail'
import ResetPassword from '@/pages/auth/ResetPassword'
import ComplianceReview from '@/pages/auth/ComplianceReview'
import SelectRole from '@/pages/auth/SelectRole'
import Dashboard from '@/pages/Dashboard'
import Profile from '@/pages/users/Profile'
import UserManagement from '@/pages/Identity/UserManagement'
import RoleManagement from '@/pages/Identity/RoleManagement'
import PermissionManagement from '@/pages/Identity/PermissionManagement'
import Localization from '@/pages/Localization'
import SystemSettings from '@/pages/SystemSettings'
import EmailTemplates from '@/pages/EmailTemplates'
import AuditLogs from '@/pages/AuditLogs'
import EmailDeliveryLogs from '@/pages/EmailDeliveryLogs'
import ComplianceTerms from '@/pages/ComplianceTerms'
import SitesPage from '@/pages/property-management/SitesPage'
import BlocksPage from '@/pages/property-management/BlocksPage'
import UnitsPage from '@/pages/property-management/UnitsPage'
import ResidentsPage from '@/pages/property-management/ResidentsPage'
import GeneralAssembliesPage from '@/pages/property-management/GeneralAssembliesPage'

import Error500 from '@/pages/error/Error500'
import Error404 from '@/pages/error/Error404'
import Error401 from '@/pages/error/Error401'
import Maintenance from '@/pages/error/Maintenance'
import ProtectedRoute from '@/components/ProtectedRoute'
import CookieNotice from '@/components/CookieNotice'
import MaintenanceGuard from '@/components/MaintenanceGuard'

import SessionTimeout from '@/components/SessionTimeout'
import { useAuth } from '@/context/AuthContext'
import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import SystemSettingsService from '@/services/systemSettingsService'

const RoleRedirectHandler = () => {
  const { userInfo, roles, activeRole, isLoading } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  useEffect(() => {
    const PUBLIC_PATHS = ['/auth/', '/confirm-email', '/reset-password', '/error']
    const isPublicPath = PUBLIC_PATHS.some(path => location.pathname.startsWith(path))

    if (!isLoading && userInfo && roles.length > 1 && !activeRole && !isPublicPath) {
      navigate('/auth/select-role')
    }
  }, [userInfo, roles, activeRole, isLoading, navigate, location.pathname])

  return null
}

const ComplianceRedirectHandler = () => {
  const { userInfo, pendingRequiredTerms, isLoading } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  useEffect(() => {
    const PUBLIC_PATHS = ['/auth/login', '/auth/register', '/auth/forgot-password', '/confirm-email', '/reset-password', '/error']
    const isPublicPath = PUBLIC_PATHS.some(path => location.pathname.startsWith(path))
    const isComplianceReviewPage = location.pathname === '/auth/compliance-review'

    if (isLoading || !userInfo) {
      return
    }

    if (pendingRequiredTerms.length > 0 && !isComplianceReviewPage && !isPublicPath) {
      navigate('/auth/compliance-review', { replace: true })
      return
    }

    if (pendingRequiredTerms.length === 0 && isComplianceReviewPage) {
      navigate('/', { replace: true })
    }
  }, [userInfo, pendingRequiredTerms, isLoading, navigate, location.pathname])

  return null
}

function App() {
  const [sessionTimeoutMinutes, setSessionTimeoutMinutes] = useState(60)
  const [sessionWarningMinutes, setSessionWarningMinutes] = useState(2)

  useEffect(() => {
    const loadPublicSettings = async () => {
      try {
        const response = await SystemSettingsService.getPublicAuthSettings()
        if (!response.success || !response.data) {
          return
        }

        const timeout = Number(response.data.sessionIdleTimeoutMinutes)
        const warning = Number(response.data.sessionWarningBeforeTimeoutMinutes)

        const normalizedTimeout = Number.isFinite(timeout) && timeout > 0
          ? Math.trunc(timeout)
          : 60
        const normalizedWarningRaw = Number.isFinite(warning) && warning > 0
          ? Math.trunc(warning)
          : 2
        const normalizedWarning = Math.max(1, Math.min(normalizedWarningRaw, Math.max(1, normalizedTimeout - 1)))

        setSessionTimeoutMinutes(normalizedTimeout)
        setSessionWarningMinutes(normalizedWarning)
      } catch {
        // Keep defaults when settings endpoint is unreachable.
      }
    }

    void loadPublicSettings()
  }, [])

  return (
    <>
      <RoleRedirectHandler />
      <ComplianceRedirectHandler />
      <SessionTimeout timeoutInMinutes={sessionTimeoutMinutes} warningInMinutes={sessionWarningMinutes} />
      <ToastContainer
        theme="colored"
        position="top-right"
        autoClose={4000}
        hideProgressBar={false}
        newestOnTop={true}
        closeOnClick
        rtl={false}
        pauseOnFocusLoss
        draggable
        pauseOnHover
        style={{ zIndex: 99999 }}
      />
      <MaintenanceGuard>
        <Routes>
          <Route path="/" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
          <Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
          <Route path="/auth/login" element={<Login />} />
          <Route path="/auth/register" element={<Register />} />
          <Route path="/auth/forgot-password" element={<ForgotPassword />} />
          <Route path="/confirm-email" element={<ConfirmMail />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          <Route path="/auth/compliance-review" element={<ProtectedRoute><ComplianceReview /></ProtectedRoute>} />
          <Route path="/auth/select-role" element={<SelectRole />} />
          <Route path="/users/profile" element={<ProtectedRoute><Profile /></ProtectedRoute>} />
          <Route path="/admin/users" element={<ProtectedRoute permission="Users.Manage"><UserManagement /></ProtectedRoute>} />
          <Route path="/admin/roles" element={<ProtectedRoute permission="Roles.Manage"><RoleManagement /></ProtectedRoute>} />
          <Route path="/admin/permissions" element={<ProtectedRoute permission="Roles.Manage"><PermissionManagement /></ProtectedRoute>} />
          <Route path="/admin/localization" element={<ProtectedRoute permission="Localization.Manage"><Localization /></ProtectedRoute>} />
          <Route path="/admin/settings" element={<ProtectedRoute permission="Settings.Manage"><SystemSettings /></ProtectedRoute>} />
          <Route path="/admin/email-templates" element={<ProtectedRoute permission="Email.Manage"><EmailTemplates /></ProtectedRoute>} />
          <Route path="/admin/audit-logs" element={<ProtectedRoute permission="Audit.Read"><AuditLogs /></ProtectedRoute>} />
          <Route path="/admin/email-delivery-logs" element={<ProtectedRoute permission="Email.Read"><EmailDeliveryLogs /></ProtectedRoute>} />
          <Route path="/admin/compliance-terms" element={<ProtectedRoute permission="Compliance.Read"><ComplianceTerms /></ProtectedRoute>} />
          <Route path="/management/properties/sites" element={<ProtectedRoute permission="Sites.Read"><SitesPage /></ProtectedRoute>} />
          <Route path="/management/properties/blocks" element={<ProtectedRoute permission="Blocks.Read"><BlocksPage /></ProtectedRoute>} />
          <Route path="/management/properties/units" element={<ProtectedRoute permission="Units.Read"><UnitsPage /></ProtectedRoute>} />
          <Route path="/management/properties/residents" element={<ProtectedRoute permission="Residents.Read"><ResidentsPage /></ProtectedRoute>} />
          <Route path="/management/properties/general-assemblies" element={<ProtectedRoute permission="Sites.Read"><GeneralAssembliesPage /></ProtectedRoute>} />
          <Route path="/management/properties" element={<Navigate to="/management/properties/sites" replace />} />

          {/* Error Pages */}
          <Route path="/error/401" element={<Error401 />} />
          <Route path="/error/500" element={<Error500 />} />
          <Route path="/maintenance" element={<Maintenance />} />

          {/* Catch-all 404 */}
          <Route path="*" element={<Error404 />} />
        </Routes>
        <CookieNotice />
      </MaintenanceGuard>
    </>
  )
}

export default App
