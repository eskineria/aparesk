import { useState } from 'react'
import { Card, CardBody, CardHeader, Nav, Tab, Row, Col, Form, Button, FormControl, FormLabel, FormGroup, Table, Badge, Alert, Modal } from 'react-bootstrap'
import type { UserSession, MfaStatus, UpdateMfaRequest } from '@/types/auth'
import { TbUserCircle, TbSettings, TbDeviceFloppy, TbLock, TbDeviceLaptop, TbX } from 'react-icons/tb'
import { LuLoader } from 'react-icons/lu'

interface AccountProps {
    profileReg: any;
    passwordReg: any;
    handleProfileSubmit: any;
    handlePasswordSubmit: any;
    onProfileSubmit: any;
    onPasswordSubmit: any;
    isProfileUpdating: boolean;
    isPasswordUpdating: boolean;
    sessions: UserSession[];
    sessionsLoading: boolean;
    revokingSessionId: string | null;
    isRevokingOthers: boolean;
    changePasswordEnabled: boolean;
    sessionManagementEnabled: boolean;
    mfaStatus: MfaStatus | null;
    isUpdatingMfa: boolean;
    hasPassword: boolean;
    onUpdateMfa: (data: UpdateMfaRequest) => Promise<boolean>;
    onSendMfaCode: (targetState: boolean) => Promise<boolean>;
    onRevokeSession: (sessionId: string) => void;
    onRevokeOtherSessions: () => void;
    t: any;
}

const Account = ({
    profileReg, passwordReg, handleProfileSubmit, handlePasswordSubmit,
    onProfileSubmit, onPasswordSubmit, isProfileUpdating, isPasswordUpdating,
    sessions, sessionsLoading, revokingSessionId, isRevokingOthers, changePasswordEnabled, sessionManagementEnabled,
    mfaStatus, isUpdatingMfa, hasPassword, onUpdateMfa, onSendMfaCode, onRevokeSession, onRevokeOtherSessions, t
}: AccountProps) => {
    const [showMfaModal, setShowMfaModal] = useState(false)
    const [mfaCurrentPassword, setMfaCurrentPassword] = useState('')
    const [mfaCode, setMfaCode] = useState('')
    const [targetMfaEnabled, setTargetMfaEnabled] = useState(false)
    const [isSendingCode, setIsSendingCode] = useState(false)
    const [codeSentAt, setCodeSentAt] = useState<number | null>(null)

    const formatDate = (value?: string) => {
        if (!value) return '-'
        return new Date(value).toLocaleString()
    }

    const getSessionStatus = (session: UserSession) => {
        if (session.isCurrent) return <Badge bg="success">{t('profile.session_status_current')}</Badge>
        if (session.isRevoked) return <Badge bg="secondary">{t('profile.session_status_revoked')}</Badge>
        if (session.isExpired) return <Badge bg="warning" text="dark">{t('profile.session_status_expired')}</Badge>
        return <Badge bg="primary">{t('profile.session_status_active')}</Badge>
    }

    const openMfaModal = (enabled: boolean) => {
        setTargetMfaEnabled(enabled)
        setMfaCurrentPassword('')
        setMfaCode('')
        setCodeSentAt(null)
        setShowMfaModal(true)
    }

    const closeMfaModal = () => {
        if (isUpdatingMfa || isSendingCode) return
        setShowMfaModal(false)
        setMfaCurrentPassword('')
        setMfaCode('')
        setCodeSentAt(null)
    }

    const submitMfaUpdate = async () => {
        const success = await onUpdateMfa({
            enabled: targetMfaEnabled,
            currentPassword: hasPassword ? mfaCurrentPassword : undefined,
            code: !hasPassword ? mfaCode : undefined
        })

        if (success) {
            closeMfaModal()
        }
    }

    const sendVerificationCode = async () => {
        setIsSendingCode(true)
        try {
            const success = await onSendMfaCode(targetMfaEnabled)
            if (success) {
                setCodeSentAt(Date.now())
            }
        } finally {
            setIsSendingCode(false)
        }
    }

    return (
        <Card>
            <Tab.Container defaultActiveKey="settings">
                <CardHeader className="card-tabs d-flex align-items-center">
                    <div className="flex-grow-1">
                        <h4 className="card-title mb-0">{t('profile.my_account')}</h4>
                    </div>
                    <Nav className="nav-tabs card-header-tabs nav-bordered">
                        <Nav.Item>
                            <Nav.Link eventKey="settings" className="cursor-pointer">
                                <span className="d-flex align-items-center">
                                    <TbSettings className="d-md-none d-block me-1" />
                                    <span className="d-none d-md-block fw-bold">{t('profile.settings')}</span>
                                </span>
                            </Nav.Link>
                        </Nav.Item>
                    </Nav>
                </CardHeader>
                <CardBody>
                    <Tab.Content>
                        <Tab.Pane eventKey="settings">
                            <h5 className="mb-3 text-uppercase bg-light-subtle p-2 border rounded border-light bg-opacity-10">
                                <div className="d-flex align-items-center">
                                    <TbUserCircle className="me-2" size={20} /> {t('profile.personal_info')}
                                </div>
                            </h5>
                            <Form onSubmit={handleProfileSubmit(onProfileSubmit)}>
                                <Row>
                                    <Col md={6}>
                                        <FormGroup className="mb-3">
                                            <FormLabel>{t('profile.first_name')}</FormLabel>
                                            <FormControl {...profileReg('firstName')} />
                                        </FormGroup>
                                    </Col>
                                    <Col md={6}>
                                        <FormGroup className="mb-3">
                                            <FormLabel>{t('profile.last_name')}</FormLabel>
                                            <FormControl {...profileReg('lastName')} />
                                        </FormGroup>
                                    </Col>
                                </Row>
                                <FormGroup className="mb-3">
                                    <FormLabel>{t('profile.email')}</FormLabel>
                                    <FormControl type="email" readOnly {...profileReg('email')} />
                                    <Form.Text className="text-muted">
                                        {t('profile.email_readonly_notice')}
                                    </Form.Text>
                                </FormGroup>
                                <div className="text-end mb-4">
                                    <Button type="submit" variant="success" disabled={isProfileUpdating}>
                                        {isProfileUpdating ? <LuLoader className="icon-spin me-1" /> : <TbDeviceFloppy className="me-1" />}
                                        {t('profile.update_profile')}
                                    </Button>
                                </div>
                            </Form>

                            <hr className="my-4" />

                            {changePasswordEnabled ? (
                                <>
                                    <h5 className="mb-3 text-uppercase bg-light-subtle p-2 border rounded border-light bg-opacity-10">
                                        <div className="d-flex align-items-center">
                                            <TbLock className="me-2" size={20} /> {t('profile.change_password')}
                                        </div>
                                    </h5>
                                    <Form onSubmit={handlePasswordSubmit(onPasswordSubmit)}>
                                        {hasPassword && (
                                            <FormGroup className="mb-3">
                                                <FormLabel>{t('profile.current_password')}</FormLabel>
                                                <FormControl type="password" {...passwordReg('currentPassword')} />
                                            </FormGroup>
                                        )}
                                        <Row>
                                            <Col md={6}>
                                                <FormGroup className="mb-3">
                                                    <FormLabel>{t('profile.new_password')}</FormLabel>
                                                    <FormControl type="password" {...passwordReg('newPassword')} />
                                                </FormGroup>
                                            </Col>
                                            <Col md={6}>
                                                <FormGroup className="mb-3">
                                                    <FormLabel>{t('profile.confirm_new_password')}</FormLabel>
                                                    <FormControl type="password" {...passwordReg('confirmNewPassword')} />
                                                </FormGroup>
                                            </Col>
                                        </Row>
                                        <div className="text-end">
                                            <Button type="submit" variant="info" disabled={isPasswordUpdating}>
                                                {isPasswordUpdating ? <LuLoader className="icon-spin me-1" /> : <TbDeviceFloppy className="me-1" />}
                                                {hasPassword ? t('profile.update_password') : t('profile.set_password')}
                                            </Button>
                                        </div>
                                    </Form>
                                </>
                            ) : (
                                <Alert variant="warning" className="mb-0">
                                    {t('profile.change_password_disabled')}
                                </Alert>
                            )}

                            <hr className="my-4" />

                            <h5 className="mb-3 text-uppercase bg-light-subtle p-2 border rounded border-light bg-opacity-10">
                                <div className="d-flex align-items-center">
                                    <TbLock className="me-2" size={20} /> {t('profile.mfa.title')}
                                </div>
                            </h5>
                            {!mfaStatus ? (
                                <Alert variant="secondary" className="mb-0">
                                    {t('profile.mfa.unavailable')}
                                </Alert>
                            ) : !mfaStatus.featureEnabled ? (
                                <Alert variant="secondary" className="mb-0">
                                    {t('profile.mfa.feature_disabled')}
                                </Alert>
                            ) : (
                                <Alert variant={mfaStatus.enabled ? 'success' : 'warning'} className="mb-0">
                                    <div className="d-flex justify-content-between align-items-start gap-3 flex-wrap">
                                        <div>
                                            <div>{mfaStatus.enabled ? t('profile.mfa.enabled_desc') : t('profile.mfa.disabled_desc')}</div>
                                            <div className="small mt-1">{t('profile.mfa.action_hint')}</div>
                                        </div>
                                        <div className="d-flex align-items-center gap-2">
                                            <Badge bg={mfaStatus.enabled ? 'success' : 'warning'} text={mfaStatus.enabled ? undefined : 'dark'}>
                                                {mfaStatus.enabled ? t('profile.mfa.enabled') : t('profile.mfa.disabled')}
                                            </Badge>
                                            {mfaStatus.enabled ? (
                                                <Button
                                                    variant="outline-danger"
                                                    size="sm"
                                                    disabled={isUpdatingMfa}
                                                    onClick={() => openMfaModal(false)}>
                                                    {t('profile.mfa.disable')}
                                                </Button>
                                            ) : (
                                                <Button
                                                    variant="outline-success"
                                                    size="sm"
                                                    disabled={isUpdatingMfa}
                                                    onClick={() => openMfaModal(true)}>
                                                    {t('profile.mfa.enable')}
                                                </Button>
                                            )}
                                        </div>
                                    </div>
                                </Alert>
                            )}

                            <hr className="my-4" />

                            {sessionManagementEnabled ? (
                                <>
                                    <div className="d-flex align-items-center justify-content-between mb-3 gap-2 flex-wrap">
                                        <h5 className="mb-0 text-uppercase bg-light-subtle p-2 border rounded border-light bg-opacity-10">
                                            <div className="d-flex align-items-center">
                                                <TbDeviceLaptop className="me-2" size={20} /> {t('profile.active_sessions')}
                                            </div>
                                        </h5>
                                        <Button
                                            variant="outline-danger"
                                            size="sm"
                                            onClick={onRevokeOtherSessions}
                                            disabled={isRevokingOthers || sessionsLoading}>
                                            {isRevokingOthers ? <LuLoader className="icon-spin me-1" /> : <TbX className="me-1" />}
                                            {t('profile.revoke_other_sessions')}
                                        </Button>
                                    </div>

                                    <div className="table-responsive">
                                        <Table hover className="align-middle mb-0">
                                            <thead>
                                                <tr>
                                                    <th>{t('profile.session_device')}</th>
                                                    <th>{t('profile.session_ip')}</th>
                                                    <th>{t('profile.session_created')}</th>
                                                    <th>{t('profile.session_last_used')}</th>
                                                    <th>{t('profile.session_expires')}</th>
                                                    <th>{t('profile.session_status')}</th>
                                                    <th className="text-end">{t('profile.session_action')}</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {sessionsLoading && (
                                                    <tr>
                                                        <td colSpan={7} className="text-center text-muted py-4">
                                                            {t('common.loading')}
                                                        </td>
                                                    </tr>
                                                )}

                                                {!sessionsLoading && sessions.length === 0 && (
                                                    <tr>
                                                        <td colSpan={7} className="text-center text-muted py-4">
                                                            {t('profile.no_sessions')}
                                                        </td>
                                                    </tr>
                                                )}

                                                {!sessionsLoading && sessions.map(session => (
                                                    <tr key={session.id}>
                                                        <td className="text-wrap" style={{ maxWidth: 260 }}>
                                                            {session.userAgent || t('profile.session_unknown_device')}
                                                        </td>
                                                        <td>{session.ipAddress || '-'}</td>
                                                        <td>{formatDate(session.createdAtUtc)}</td>
                                                        <td>{formatDate(session.lastUsedAtUtc)}</td>
                                                        <td>{formatDate(session.expiresAtUtc)}</td>
                                                        <td>{getSessionStatus(session)}</td>
                                                        <td className="text-end">
                                                            {!session.isCurrent && !session.isRevoked && !session.isExpired && (
                                                                <Button
                                                                    variant="outline-danger"
                                                                    size="sm"
                                                                    onClick={() => onRevokeSession(session.id)}
                                                                    disabled={revokingSessionId === session.id}>
                                                                    {revokingSessionId === session.id ? <LuLoader className="icon-spin me-1" /> : null}
                                                                    {t('profile.session_revoke')}
                                                                </Button>
                                                            )}
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </Table>
                                    </div>
                                </>
                            ) : (
                                <Alert variant="warning" className="mb-0">
                                    {t('profile.session_management_disabled')}
                                </Alert>
                            )}
                        </Tab.Pane>
                    </Tab.Content>
                </CardBody>
            </Tab.Container>

            <Modal show={showMfaModal} onHide={closeMfaModal} centered>
                <Modal.Header closeButton={!isUpdatingMfa}>
                    <Modal.Title>
                        {targetMfaEnabled ? t('profile.mfa.enable') : t('profile.mfa.disable')}
                    </Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <p className="text-muted mb-3">
                        {targetMfaEnabled ? t('profile.mfa.enable_confirm') : t('profile.mfa.disable_confirm')}
                    </p>
                    {hasPassword ? (
                        <FormGroup>
                            <FormLabel>{t('profile.current_password')}</FormLabel>
                            <FormControl
                                type="password"
                                value={mfaCurrentPassword}
                                onChange={(event) => setMfaCurrentPassword(event.target.value)}
                                disabled={isUpdatingMfa}
                            />
                        </FormGroup>
                    ) : (
                        <div className="mt-2">
                            <p className="small text-info mb-2">
                                {t('profile.mfa.social_info')}
                            </p>
                            <div className="d-flex gap-2 align-items-end mb-3">
                                <FormGroup className="flex-grow-1">
                                    <FormLabel>{t('profile.mfa.verification_code')}</FormLabel>
                                    <FormControl
                                        type="text"
                                        placeholder="123456"
                                        value={mfaCode}
                                        onChange={(event) => setMfaCode(event.target.value)}
                                        disabled={isUpdatingMfa || isSendingCode}
                                    />
                                </FormGroup>
                                <Button
                                    variant="outline-primary"
                                    onClick={sendVerificationCode}
                                    // eslint-disable-next-line react-hooks/purity
                                    disabled={isSendingCode || isUpdatingMfa || (!!codeSentAt && (Date.now() - codeSentAt < 60000))}
                                >
                                    {isSendingCode ? <LuLoader className="icon-spin me-1" /> : null}
                                    {codeSentAt ? t('profile.mfa.resend_code') : t('profile.mfa.send_code')}
                                </Button>
                            </div>
                            {codeSentAt && (
                                <Alert variant="info" className="small py-1 px-2 mb-0">
                                    {t('profile.mfa.code_sent_info')}
                                </Alert>
                            )}
                        </div>
                    )}
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={closeMfaModal} disabled={isUpdatingMfa || isSendingCode}>
                        {t('common.cancel')}
                    </Button>
                    <Button
                        variant={targetMfaEnabled ? 'success' : 'danger'}
                        onClick={() => { void submitMfaUpdate() }}
                        disabled={isUpdatingMfa || isSendingCode || (hasPassword ? mfaCurrentPassword.trim().length === 0 : mfaCode.trim().length === 0)}>
                        {isUpdatingMfa ? <LuLoader className="icon-spin me-1" /> : null}
                        {t('common.confirm')}
                    </Button>
                </Modal.Footer>
            </Modal>
        </Card>
    )
}

export default Account
