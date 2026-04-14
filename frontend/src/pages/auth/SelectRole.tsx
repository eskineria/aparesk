import { useState } from 'react'
import { Card, CardBody, Col, Row, Button } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { LuCheck, LuLoader, LuShield, LuArrowLeft } from 'react-icons/lu'
import { useAuth } from '@/context/AuthContext'
import AppLogo from '@/components/AppLogo'
import LanguageDropdown from '@/components/LanguageDropdown'
import { author, currentYear } from '@/helpers'
import { useBranding } from '@/context/BrandingContext'

const SelectRole = () => {
    const { t } = useTranslation()
    const { applicationName } = useBranding()
    const navigate = useNavigate()
    const { roles, switchRole, activeRole } = useAuth()
    const [selectedRole, setSelectedRole] = useState<string>(activeRole || '')
    const [isSubmitting, setIsSubmitting] = useState(false)

    const handleConfirm = async () => {
        if (!selectedRole) return
        setIsSubmitting(true)
        try {
            const switched = await switchRole(selectedRole)
            if (switched) {
                window.location.replace('/dashboard')
            }
        } catch (error) {
            // Error handled by AuthContext/toast
        } finally {
            setIsSubmitting(false)
        }
    }

    if (roles.length <= 1 && activeRole) {
        navigate('/dashboard')
        return null
    }

    return (
        <div className="auth-box p-0 w-100">
            <Row className="w-100 g-0">
                <Col md={'auto'}>
                    <Card className="auth-box-form border-0 mb-0">
                        <CardBody className="min-vh-100 d-flex flex-column justify-content-center">
                            <div className="d-flex justify-content-end mb-2">
                                <LanguageDropdown />
                            </div>
                            <div className="auth-brand mb-0 text-center">
                                <AppLogo />
                            </div>
                            <div className="mt-auto">
                                <h3 className="text-center">{t('auth.selectRole.title')}</h3>
                                <p className="text-muted text-center auth-sub-text mx-auto">
                                    {t('auth.selectRole.subtitle')}
                                </p>

                                <div className="mt-4">
                                    <div className="role-selection-list">
                                        {roles.map((role) => (
                                            <div
                                                key={role}
                                                className={`role-item p-3 mb-2 rounded border d-flex align-items-center justify-content-between ${selectedRole === role ? 'border-primary bg-primary bg-opacity-10 shadow-sm' : 'border-light-subtle'}`}
                                                onClick={() => setSelectedRole(role)}
                                            >
                                                <div className="d-flex align-items-center gap-3">
                                                    <div className={`role-icon-box p-2 rounded-circle ${selectedRole === role ? 'bg-primary text-white' : 'bg-light text-muted'}`}>
                                                        <LuShield size={20} />
                                                    </div>
                                                    <div>
                                                        <h6 className="mb-0 fw-semibold">{role}</h6>
                                                        <small className="text-muted">{selectedRole === role ? t('topbar.active_role') : ''}</small>
                                                    </div>
                                                </div>
                                                {selectedRole === role && <LuCheck className="text-primary" size={20} />}
                                            </div>
                                        ))}
                                    </div>

                                    <div className="d-grid mt-4">
                                        <Button
                                            variant="primary"
                                            className="fw-semibold py-2"
                                            disabled={!selectedRole || isSubmitting}
                                            onClick={handleConfirm}
                                        >
                                            {isSubmitting ? <LuLoader className="icon-spin me-1" /> : null}
                                            {isSubmitting ? t('common.loading') : t('auth.selectRole.submitButton')}
                                        </Button>
                                    </div>

                                    <div className="text-center mt-3">
                                        <Link to="/auth/login" className="text-muted text-decoration-none small d-flex align-items-center justify-content-center gap-1">
                                            <LuArrowLeft size={14} />
                                            {t('auth.selectRole.backToLogin')}
                                        </Link>
                                    </div>
                                </div>
                            </div>
                            <p className="text-center text-muted mt-auto mb-0">
                                © {currentYear} {applicationName} — by <span className="fw-semibold">{author}</span>
                            </p>
                        </CardBody>
                    </Card>
                </Col>
                <Col>
                    <div className="h-100 position-relative card-side-img rounded-0 overflow-hidden" style={{ background: 'linear-gradient(135deg, #6366f1 0%, #a855f7 100%)' }}>
                        <div className="p-4 card-img-overlay auth-overlay d-flex align-items-center justify-content-center">
                            <div className="text-center text-white p-5">
                                <LuShield size={80} className="mb-4 opacity-50" />
                                <h2 className="display-6 fw-bold mb-3">{t('auth.selectRole.title')}</h2>
                                <p className="fs-18 opacity-75">{t('auth.selectRole.subtitle')}</p>
                            </div>
                        </div>
                    </div>
                </Col>
            </Row>
        </div>
    )
}

export default SelectRole
