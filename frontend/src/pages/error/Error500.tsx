import { author, currentYear } from '@/helpers'
import { Card, Col, Container, Row, Button } from 'react-bootstrap'
import AppLogo from '@/components/AppLogo'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useBranding } from '@/context/BrandingContext'

const Error500 = () => {
    const navigate = useNavigate()
    const { t, i18n } = useTranslation()
    const { applicationName } = useBranding()
    const isTurkish = (i18n.resolvedLanguage || i18n.language || '').toLowerCase().startsWith('tr')

    const resolveSafeText = (key: string, fallbackTr: string, fallbackEn: string) => {
        const translated = t(key)
        if (!translated || translated === key) {
            return isTurkish ? fallbackTr : fallbackEn
        }

        return translated
    }

    const title = resolveSafeText('error.500.title', 'Sunucu Hatası', 'Internal Server Error')
    const message = resolveSafeText(
        'error.500.message',
        'İsteğiniz işlenirken teknik bir sorun oluştu. Lütfen daha sonra tekrar deneyin.',
        'Something went wrong on our end. Please try again later.'
    )
    const retry = resolveSafeText('error.500.retry', 'Tekrar Dene', 'Retry')
    const support = resolveSafeText('error.500.support', 'Destek Al', 'Contact Support')

    return (
        <div className="auth-box overflow-hidden align-items-center d-flex min-vh-100">
            <Container>
                <Row className="justify-content-center">
                    <Col xxl={4} md={6} sm={8}>
                        <Card className="p-4 border-0 shadow-lg">
                            <div className="position-absolute top-0 end-0" style={{ width: 180 }}>
                                <svg
                                    style={{ opacity: '0.075', width: '100%', height: 'auto' }}
                                    width={600}
                                    height={560}
                                    viewBox="0 0 600 560"
                                    fill="none"
                                    xmlns="http://www.w3.org/2000/svg">
                                    <g clipPath="url(#clip0_948_1464)">
                                        <mask id="mask0_948_1464" style={{ maskType: 'luminance' }} maskUnits="userSpaceOnUse" x={0} y={0} width={600} height={1200}>
                                            <path d="M0 0L0 1200H600L600 0H0Z" fill="white" />
                                        </mask>
                                        <g mask="url(#mask0_948_1464)">
                                            <path d="M537.448 166.697L569.994 170.892L550.644 189.578L537.448 166.697Z" fill="#FF4C3E" />
                                        </g>
                                        <mask id="mask1_948_1464" style={{ maskType: 'luminance' }} maskUnits="userSpaceOnUse" x={0} y={0} width={600} height={1200}>
                                            <path d="M0 0L0 1200H600L600 0H0Z" fill="white" />
                                        </mask>
                                        <g mask="url(#mask1_948_1464)">
                                            <path
                                                d="M364.093 327.517L332.306 359.304C321.885 369.725 304.989 369.725 294.568 359.304L262.781 327.517C252.36 317.096 252.36 300.2 262.781 289.779L294.568 257.992C304.989 247.571 321.885 247.571 332.306 257.992L364.093 289.779C374.514 300.2 374.514 317.096 364.093 327.517Z"
                                                stroke="#089df1"
                                                strokeWidth={2}
                                                strokeMiterlimit={10}
                                            />
                                            {/* ... simplified paths or keep them as is from ubold ... */}
                                            <path
                                                d="M377.923 101.019L315.106 163.836C299.517 179.425 274.242 179.425 258.653 163.836L195.836 101.019C180.247 85.4301 180.247 60.1551 195.836 44.5661L258.653 -18.251C274.242 -33.84 299.517 -33.84 315.106 -18.251L377.923 44.5661C393.512 60.1551 393.512 85.4301 377.923 101.019Z"
                                                stroke="#089df1"
                                                strokeWidth={2}
                                                strokeMiterlimit={10}
                                            />
                                        </g>
                                    </g>
                                    <defs>
                                        <clipPath id="clip0_948_1464">
                                            <rect width={560} height={600} fill="white" transform="matrix(0 -1 1 0 0 560)" />
                                        </clipPath>
                                    </defs>
                                </svg>
                            </div>
                            <div className="auth-brand text-center mb-2">
                                <AppLogo />
                            </div>
                            <div className="p-4 text-center">
                                <div className="error-text-alt fs-72 text-danger fw-bold">500</div>
                                <h3 className="fw-bold text-uppercase">{title}</h3>
                                <p className="text-muted fs-15">{message}</p>
                                <div className="mt-4 d-flex justify-content-center gap-2">
                                    <Button variant="primary" onClick={() => navigate('/')}>
                                        {retry}
                                    </Button>
                                    <Button variant="outline-info">
                                        {support}
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

export default Error500
