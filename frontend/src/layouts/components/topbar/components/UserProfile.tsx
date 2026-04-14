import { userDropdownItems } from '@/layouts/components/data'

import { Link, useNavigate } from "react-router-dom";
import { Fragment } from 'react'
import { Dropdown, DropdownDivider, DropdownItem, DropdownMenu, DropdownToggle } from 'react-bootstrap'
import { TbChevronDown } from 'react-icons/tb'
import { useTranslation } from 'react-i18next'
import { AuthService } from '@/services/authService'
import { useAuth } from '@/context/AuthContext'

import defaultUser from '@/assets/images/users/user-3.jpg'

const UserProfile = () => {
    const { t } = useTranslation()
    const navigate = useNavigate()
    const { userInfo } = useAuth()

    const handleLogout = async () => {
        try {
            await AuthService.logout()
            navigate('/auth/login')
        } catch (error) {
            navigate('/auth/login')
        }
    }

    return (
        <div className="topbar-item nav-user">
            <Dropdown align="end">
                <DropdownToggle as={'a'} role="button" className="topbar-link dropdown-toggle drop-arrow-none px-2 border-0 bg-transparent">
                    <img src={userInfo?.profilePicture || defaultUser} width="32" height="32" className="rounded-circle me-lg-2 d-flex" alt="" style={{ objectFit: 'cover' }} />
                    <div className="d-lg-flex align-items-center gap-1 d-none">
                        <h5 className="my-0">{userInfo?.firstName || t('topbar.user')}</h5>
                        <TbChevronDown className="align-middle" />
                    </div>
                </DropdownToggle>
                <DropdownMenu className="dropdown-menu-end shadow-sm border-0">
                    {userDropdownItems.map((item, idx) => (
                        <Fragment key={idx}>
                            {item.isHeader ? (
                                <div className="dropdown-header noti-title">
                                    <h6 className="text-overflow m-0">{item.labelKey ? t(item.labelKey) : item.label}</h6>
                                </div>
                            ) : item.isDivider ? (
                                <DropdownDivider />
                            ) : item.labelKey === 'topbar.logout' ? (
                                <DropdownItem as="button" onClick={handleLogout} className={item.class}>
                                    {item.icon && <item.icon className="me-2 fs-17 align-middle" />}
                                    <span className="align-middle">
                                        {t(item.labelKey)}
                                    </span>
                                </DropdownItem>
                            ) : (
                                <DropdownItem as={Link} to={item.url ?? ''} className={item.class}>
                                    {item.icon && <item.icon className="me-2 fs-17 align-middle" />}
                                    <span className="align-middle">
                                        {item.labelKey ? t(item.labelKey) : item.label}
                                    </span>
                                </DropdownItem>
                            )}
                        </Fragment>
                    ))}
                </DropdownMenu>
            </Dropdown>
        </div>
    )
}

export default UserProfile
