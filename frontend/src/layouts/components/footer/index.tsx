import { author, currentYear } from '@/helpers'
import { Col, Container, Row } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useBranding } from '@/context/BrandingContext'

const Footer = () => {
    const { t } = useTranslation()
    const { applicationName } = useBranding()

    return (
        <footer className="footer text-center">
            <Container fluid>
                <Row>
                    <Col xs={12}>
                        © {currentYear} {applicationName} -  {t('common.developed_by', { author: author })}
                    </Col>
                </Row>
            </Container>
        </footer>
    )
}

export default Footer
