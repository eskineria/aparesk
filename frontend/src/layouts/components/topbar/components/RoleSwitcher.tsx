import { useMemo, useState } from 'react'
import { Button, Modal, Spinner } from 'react-bootstrap'
import { TbChevronDown } from 'react-icons/tb'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/context/AuthContext'

const RoleSwitcher = () => {
    const { t } = useTranslation()
    const { roles, activeRole, switchRole } = useAuth()
    const [showModal, setShowModal] = useState(false)
    const [isSwitching, setIsSwitching] = useState(false)
    const [pendingRole, setPendingRole] = useState<string | null>(null)

    const uniqueRoles = useMemo(() => Array.from(new Set(roles)), [roles])
    const currentRole = activeRole || uniqueRoles[0] || ''
    const alternativeRoles = useMemo(
        () => uniqueRoles.filter((role) => role !== currentRole),
        [uniqueRoles, currentRole]
    )
    const canSwitchRole = alternativeRoles.length > 0

    const handleSelectRole = async (roleName: string) => {
        if (!roleName || !canSwitchRole || isSwitching) return
        setIsSwitching(true)
        setPendingRole(roleName)
        try {
            const switched = await switchRole(roleName)
            if (switched) {
                setShowModal(false)
                window.location.replace('/dashboard')
            }
        } finally {
            setIsSwitching(false)
            setPendingRole(null)
        }
    }

    return (
        <div className="topbar-item nav-user role-switcher d-none d-md-flex">
            <button
                type="button"
                className="topbar-link dropdown-toggle drop-arrow-none px-2 border-0 bg-transparent role-switcher-trigger"
                onClick={() => canSwitchRole && setShowModal(true)}
                disabled={!canSwitchRole || isSwitching}
                title={t('topbar.active_role')}
                aria-label={t('topbar.select_role')}
            >
                <div className="d-flex align-items-center gap-2">
                    <span className="role-switcher-avatar">
                        {(currentRole || t('topbar.no_role')).charAt(0).toUpperCase()}
                    </span>
                    <div className="d-lg-flex flex-column d-none">
                        <span className="role-switcher-label">{t('topbar.active_role')}</span>
                        <span className="fw-semibold role-switcher-value">{currentRole || t('topbar.no_role')}</span>
                    </div>
                    {canSwitchRole && <TbChevronDown className="align-middle d-none d-lg-block" />}
                </div>
            </button>

            <Modal
                show={showModal}
                onHide={() => !isSwitching && setShowModal(false)}
                centered
            >
                <Modal.Header closeButton={!isSwitching}>
                    <div>
                        <Modal.Title>{t('topbar.select_role')}</Modal.Title>
                        <p className="text-muted mb-0 mt-1 fs-sm">{t('topbar.role_switch_subtitle')}</p>
                    </div>
                </Modal.Header>
                <Modal.Body>
                    <div className="role-switcher-modal">
                        <div className="role-switcher-current">
                            <small className="text-muted d-block mb-1 text-uppercase fw-semibold">
                                {t('topbar.current_role')}
                            </small>
                            <div className="d-flex align-items-center gap-2">
                                <span className="role-switcher-option-avatar">
                                    {(currentRole || t('topbar.no_role')).charAt(0).toUpperCase()}
                                </span>
                                <span className="fw-semibold">{currentRole || t('topbar.no_role')}</span>
                            </div>
                        </div>

                        <div className="mt-3 mb-2 text-muted fw-semibold fs-sm">
                            {t('topbar.available_roles')}
                        </div>

                        {alternativeRoles.length === 0 ? (
                            <div className="alert alert-light border mb-0">
                                {t('topbar.no_other_roles')}
                            </div>
                        ) : (
                            <div className="d-flex flex-column gap-2">
                                {alternativeRoles.map((role) => {
                                    const isPending = pendingRole === role
                                    return (
                                        <Button
                                            key={role}
                                            variant="light"
                                            className="role-switcher-option d-flex justify-content-between align-items-center border rounded-3"
                                            disabled={isSwitching}
                                            onClick={() => handleSelectRole(role)}
                                        >
                                            <span className="d-flex align-items-center gap-2">
                                                <span className="role-switcher-option-avatar">
                                                    {role.charAt(0).toUpperCase()}
                                                </span>
                                                <span className="fw-semibold">{role}</span>
                                            </span>
                                            {isPending ? (
                                                <Spinner animation="border" size="sm" />
                                            ) : (
                                                <span className="badge bg-primary-subtle text-primary">
                                                    {t('topbar.select_role')}
                                                </span>
                                            )}
                                        </Button>
                                    )
                                })}
                            </div>
                        )}
                    </div>
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="light" onClick={() => setShowModal(false)} disabled={isSwitching}>
                        {t('common.cancel')}
                    </Button>
                </Modal.Footer>
            </Modal>
        </div>
    )
}

export default RoleSwitcher
