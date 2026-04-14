import { Card, CardBody } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { TbMail, TbShieldCheck, TbUserCircle } from 'react-icons/tb'

import type { UserInfo } from '@/types/auth'

type ProfileInfoProps = {
    userInfo: UserInfo | null
}

const getInitials = (firstName?: string | null, lastName?: string | null) => {
    const first = firstName?.trim().charAt(0) ?? ''
    const last = lastName?.trim().charAt(0) ?? ''
    return `${first}${last}`.toUpperCase() || 'U'
}

const ProfileInfo = ({ userInfo }: ProfileInfoProps) => {
    const { t } = useTranslation()

    return (
        <Card className="card-top-sticky">
            <CardBody>
                <div className="text-center mb-4">
                    <div className="mx-auto mb-3 position-relative" style={{ width: '88px', height: '88px' }}>
                        {userInfo?.profilePicture ? (
                            <img
                                src={userInfo.profilePicture}
                                alt={userInfo.email}
                                className="img-fluid rounded-circle border"
                                style={{ width: '88px', height: '88px', objectFit: 'cover' }}
                            />
                        ) : (
                            <div
                                className="avatar-xl bg-primary-subtle rounded-circle d-flex align-items-center justify-content-center text-primary fw-bold display-6"
                                style={{ width: '88px', height: '88px' }}>
                                {getInitials(userInfo?.firstName, userInfo?.lastName)}
                            </div>
                        )}
                    </div>

                    <h4 className="mb-1">
                        {[userInfo?.firstName, userInfo?.lastName].filter(Boolean).join(' ').trim() || t('profile.title')}
                    </h4>
                    <p className="text-muted mb-0">{userInfo?.email || '-'}</p>
                </div>

                <div className="border rounded p-3">
                    <div className="d-flex align-items-center gap-2 mb-3">
                        <TbUserCircle size={18} className="text-primary" />
                        <div>
                            <div className="small text-muted">{t('identity.table.active_role')}</div>
                            <div className="fw-semibold">{userInfo?.activeRole || '-'}</div>
                        </div>
                    </div>

                    <div className="d-flex align-items-center gap-2 mb-3">
                        <TbShieldCheck size={18} className="text-success" />
                        <div>
                            <div className="small text-muted">{t('identity.table.assigned_roles')}</div>
                            <div className="fw-semibold">
                                {userInfo?.roles?.length ? userInfo.roles.join(', ') : '-'}
                            </div>
                        </div>
                    </div>

                    <div className="d-flex align-items-center gap-2">
                        <TbMail size={18} className="text-info" />
                        <div>
                            <div className="small text-muted">{t('profile.email')}</div>
                            <div className="fw-semibold">{userInfo?.email || '-'}</div>
                        </div>
                    </div>
                </div>
            </CardBody>
        </Card>
    )
}

export default ProfileInfo
