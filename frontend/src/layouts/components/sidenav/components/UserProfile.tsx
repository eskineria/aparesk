import { userDropdownItems } from '@/layouts/components/data'
import { Link, useNavigate } from "react-router-dom";
import { Fragment } from 'react'
import { Dropdown, DropdownDivider, DropdownItem, DropdownMenu, DropdownToggle } from 'react-bootstrap'
import { TbSettings } from 'react-icons/tb'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/context/AuthContext'
import { AuthService } from '@/services/authService'
import defaultUser from '@/assets/images/users/user-3.jpg'

const UserProfile = () => {
    const { t } = useTranslation()
    const navigate = useNavigate()
    const { userInfo, roles, activeRole } = useAuth()
    const currentRole = activeRole || roles[0] || ''

    const handleLogout = async () => {
        try {
            await AuthService.logout()
            navigate('/auth/login')
        } catch (error) {
            navigate('/auth/login')
        }
    }

    return (
        <div className="sidenav-user">
            <div className="d-flex justify-content-between align-items-center">
                <div className="flex-grow-1 overflow-hidden">
                    <Link to="/users/profile" className="link-reset text-decoration-none d-flex align-items-center gap-2">
                        <img
                            src={userInfo?.profilePicture || defaultUser}
                            alt=""
                            width="36"
                            height="36"
                            className="rounded-circle avatar-md shadow-sm object-fit-cover"
                        />
                        <div className="overflow-hidden">
                            <span className="sidenav-user-name fw-bold d-block text-dark text-truncate">
                                {userInfo?.fullName || t('topbar.user')}
                            </span>
                            <span className="fs-12 fw-semibold text-muted text-truncate d-block">
                                {currentRole || t('topbar.no_role')}
                            </span>
                        </div>
                    </Link>
                </div>
                <Dropdown>
                    <DropdownToggle
                        as={'a'}
                        role="button"
                        className="dropdown-toggle drop-arrow-none link-reset sidenav-user-set-icon text-secondary">
                        <TbSettings size={22} className="align-middle ms-1" />
                    </DropdownToggle>

                    <DropdownMenu className="shadow border-0 mt-1 dropdown-menu-end">
                        <div className="px-3 pt-3 pb-2 border-bottom bg-light bg-opacity-50">
                            <div className="d-flex justify-content-between align-items-center mb-2">
                                <span className="text-muted fs-11 fw-bold text-uppercase">{t('topbar.active_role')}</span>
                                <span className="badge bg-primary bg-opacity-10 text-primary border border-primary border-opacity-25 text-truncate" style={{ maxWidth: '160px' }}>
                                    {currentRole || t('topbar.no_role')}
                                </span>
                            </div>
                        </div>

                        {userDropdownItems.map((item, idx) => (
                            <Fragment key={idx}>
                                {item.isHeader ? (
                                    <div className="dropdown-header noti-title px-3 py-2">
                                        <h6 className="text-overflow m-0 small fw-bold text-uppercase">
                                            {item.labelKey ? t(item.labelKey) : item.label}
                                        </h6>
                                    </div>
                                ) : item.isDivider ? (
                                    <DropdownDivider className="m-0" />
                                ) : item.labelKey === 'topbar.logout' ? (
                                    <DropdownItem as="button" onClick={handleLogout} className={`px-3 py-2 ${item.class || ''}`}>
                                        {item.icon && <item.icon className="me-2 fs-17 align-middle" />}
                                        <span className="align-middle small">{t(item.labelKey)}</span>
                                    </DropdownItem>
                                ) : (
                                    <DropdownItem as={Link} to={item.url ?? ''} className={`px-3 py-2 ${item.class || ''}`}>
                                        {item.icon && <item.icon className={`me-2 fs-17 align-middle ${!item.class ? 'text-muted' : ''}`} />}
                                        <span className="align-middle small">
                                            {item.labelKey ? t(item.labelKey) : item.label}
                                        </span>
                                    </DropdownItem>
                                )}
                            </Fragment>
                        ))}
                    </DropdownMenu>
                </Dropdown>
            </div>
        </div>
    )
}

export default UserProfile
