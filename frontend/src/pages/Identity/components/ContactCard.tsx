import { Link } from 'react-router-dom'
import { Card, CardBody, Button, Badge } from 'react-bootstrap'
import type { UserListDto } from '@/types/accessControl'
import { LuUserCog, LuUserX, LuUserCheck } from 'react-icons/lu'
import { useTranslation } from 'react-i18next'

interface Props {
    user: UserListDto;
    onManageRoles: (user: UserListDto) => void;
    onToggleStatus: (user: UserListDto) => void;
}

const ContactCard = ({ user, onManageRoles, onToggleStatus }: Props) => {
    const { t } = useTranslation()
    const { firstName, lastName, email, activeRole, isActive, profilePicture, roles } = user
    const fullName = `${firstName} ${lastName}`

    return (
        <Card>
            <CardBody className="text-center">
                {profilePicture ? (
                    <img src={profilePicture} alt="" className="rounded-circle mb-2" width={72} height={72} />
                ) : (
                    <div className="avatar-xl mx-auto bg-primary-subtle text-primary rounded-circle d-flex align-items-center justify-content-center mb-2 fs-1 fw-bold" style={{ width: '72px', height: '72px' }}>
                        {firstName?.[0]}{lastName?.[0]}
                    </div>
                )}

                <h5 className="mb-0 mt-2">
                    <Link to="/users/profile" className="link-reset">{fullName}</Link>
                </h5>
                <span className="text-muted fs-xs">{activeRole || t('identity.no_active_role')}</span><br />
                <span className={`badge bg-${isActive ? 'success' : 'danger'} my-1`}>
                    {isActive ? t('identity.status_active') : t('identity.status_inactive')}
                </span><br />
                <span className="text-muted">{email}</span>

                <div className="mt-3">
                    <Button variant="primary" size="sm" className="me-1" onClick={() => onManageRoles(user)}>
                        <LuUserCog className="me-1" /> {t('identity.roles')}
                    </Button>
                    <Button
                        variant={isActive ? "outline-secondary" : "outline-success"}
                        size="sm"
                        onClick={() => onToggleStatus(user)}
                    >
                        {isActive ? <LuUserX className="me-1" /> : <LuUserCheck className="me-1" />}
                        {isActive ? t('common.deactivate') : t('common.activate')}
                    </Button>
                </div>

                {roles && roles.length > 0 && (
                    <>
                        <hr className="my-3 border-dashed" />
                        <div className="d-flex flex-wrap gap-1 justify-content-center">
                            {roles.map((role) => (
                                <Badge key={role} bg={role === 'Admin' ? 'danger-subtle' : 'info-subtle'} className={`text-${role === 'Admin' ? 'danger' : 'info'}`}>
                                    {role}
                                </Badge>
                            ))}
                        </div>
                    </>
                )}
            </CardBody>
        </Card>
    )
}

export default ContactCard
