import { author, currentYear } from '@/helpers'
import { Card, Col, Container, Row, Button } from 'react-bootstrap'
import AppLogo from '@/components/AppLogo'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { LuShieldAlert } from 'react-icons/lu'
import { useBranding } from '@/context/BrandingContext'

const Error401 = () => {
    const navigate = useNavigate()
    const { t } = useTranslation()
    const { applicationName } = useBranding()

    return (
        <div className="auth-box overflow-hidden align-items-center d-flex min-vh-100">
            <Container>
                <Row className="justify-content-center">
                    <Col xxl={4} md={6} sm={8}>
                        <Card className="p-4 border-0 shadow-lg">
                            <div className="position-absolute top-0 end-0 p-3">
                                <LuShieldAlert size={80} className="text-danger opacity-10" />
                            </div>
                            <div className="auth-brand text-center mb-2">
                                <AppLogo />
                            </div>
                            <div className="p-4 text-center">
                                <div className="error-text-alt fs-72 text-danger fw-bold">401</div>
                                <h3 className="fw-bold text-uppercase">{t('error.401.title')}</h3>
                                <p className="text-muted fs-15">
                                    {t('error.401.message')}
                                </p>
                                <div className="mt-4 d-flex justify-content-center gap-2">
                                    <Button variant="primary" onClick={() => navigate('/')}>
                                        <i className="ti ti-home me-1"></i> {t('error.401.goBack')}
                                    </Button>
                                </div>
                            </div>
                        </Card>
                        <p className="text-center text-muted mt-4 mb-0">
                            © {currentYear} {applicationName} — by <span className="fw-semibold">{author}</span>
                        </p>
                    </Col>
                </Row>
            </Container>
        </div>
    )
}

export default Error401
