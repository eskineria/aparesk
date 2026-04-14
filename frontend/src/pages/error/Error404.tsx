import { author, currentYear } from '@/helpers'
import { Card, Col, Container, Row, Button } from 'react-bootstrap'
import AppLogo from '@/components/AppLogo'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useBranding } from '@/context/BrandingContext'

const Error404 = () => {
    const navigate = useNavigate()
    const { t } = useTranslation()
    const { applicationName } = useBranding()

    return (
        <div className="auth-box overflow-hidden align-items-center d-flex min-vh-100">
            <Container>
                <Row className="justify-content-center">
                    <Col xxl={4} md={6} sm={8}>
                        <Card className="p-4 border-0 shadow-lg">
                            <div className="position-absolute top-0 end-0" style={{ width: 180 }}>
                                {/* SVG from Ubold */}
                                <svg style={{ opacity: '0.075', width: '100%', height: 'auto' }} width={600} height={560} viewBox="0 0 600 560" fill="none" xmlns="http://www.w3.org/2000/svg">
                                    <g clipPath="url(#clip0_404)">
                                        <mask id="mask0_404" style={{ maskType: 'luminance' }} maskUnits="userSpaceOnUse" x={0} y={0} width={600} height={1200}>
                                            <path d="M0 0L0 1200H600L600 0H0Z" fill="white" />
                                        </mask>
                                        <g mask="url(#mask0_404)">
                                            <path d="M537.448 166.697L569.994 170.892L550.644 189.578L537.448 166.697Z" fill="#F9BF59" />
                                        </g>
                                    </g>
                                    <defs>
                                        <clipPath id="clip0_404">
                                            <rect width={560} height={600} fill="white" transform="matrix(0 -1 1 0 0 560)" />
                                        </clipPath>
                                    </defs>
                                </svg>
                            </div>
                            <div className="auth-brand text-center mb-2">
                                <AppLogo />
                            </div>
                            <div className="p-4 text-center">
                                <div className="error-text-alt fs-72 text-warning fw-bold">404</div>
                                <h3 className="fw-bold text-uppercase">{t('error.404.title')}</h3>
                                <p className="text-muted fs-15">
                                    {t('error.404.message')}
                                </p>
                                <div className="mt-4 d-flex justify-content-center gap-2">
                                    <Button variant="primary" onClick={() => navigate('/')}>
                                        {t('error.404.goBack')}
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

export default Error404
