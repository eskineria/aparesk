import DOMPurify from 'dompurify'
import { useEffect, useMemo, useState } from 'react'
import { Button, Modal } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { ComplianceService } from '@/services/complianceService'
import type { TermsDto } from '@/types/compliance'

const CookieNotice = () => {
    const { t } = useTranslation()
    const [cookiePolicy, setCookiePolicy] = useState<TermsDto | null>(null)
    const [isVisible, setIsVisible] = useState(false)
    const [showModal, setShowModal] = useState(false)

    const storageKey = useMemo(() => {
        if (!cookiePolicy) {
            return null
        }

        return `cookie-policy-dismissed:${cookiePolicy.id}:${cookiePolicy.version}`
    }, [cookiePolicy])

    useEffect(() => {
        const loadCookiePolicy = async () => {
            try {
                const response = await ComplianceService.getActiveTermsOptional('CookiePolicy')
                if (!response.success || !response.data) {
                    return
                }

                const policy = response.data
                const nextStorageKey = `cookie-policy-dismissed:${policy.id}:${policy.version}`
                setCookiePolicy(policy)

                if (!window.localStorage.getItem(nextStorageKey)) {
                    setIsVisible(true)
                }
            } catch {
                // Cookie notice is best-effort.
            }
        }

        void loadCookiePolicy()
    }, [])

    const dismissNotice = () => {
        if (storageKey) {
            window.localStorage.setItem(storageKey, 'true')
        }

        setIsVisible(false)
        setShowModal(false)
    }

    if (!cookiePolicy || !isVisible) {
        return null
    }

    return (
        <>
            <div className="position-fixed start-0 end-0 bottom-0 p-3" style={{ zIndex: 1080 }}>
                <div
                    className="mx-auto bg-white border rounded-4 shadow-lg p-3 p-lg-4"
                    style={{ maxWidth: 980 }}
                >
                    <div className="d-flex flex-column flex-lg-row align-items-lg-center gap-3">
                        <div className="flex-grow-1">
                            <div className="fw-semibold mb-1">{t('cookieNotice.title')}</div>
                            <div className="text-muted small">
                                {cookiePolicy.summary || t('cookieNotice.message')}
                            </div>
                        </div>
                        <div className="d-flex flex-wrap gap-2">
                            <Button variant="light" onClick={() => setShowModal(true)}>
                                {t('cookieNotice.viewPolicy')}
                            </Button>
                            <Button variant="primary" onClick={dismissNotice}>
                                {t('cookieNotice.dismiss')}
                            </Button>
                        </div>
                    </div>
                </div>
            </div>

            <Modal show={showModal} onHide={() => setShowModal(false)} size="lg" centered>
                <Modal.Header closeButton className="border-bottom">
                    <Modal.Title className="fw-semibold">{t('cookieNotice.modalTitle')}</Modal.Title>
                </Modal.Header>
                <Modal.Body style={{ maxHeight: '60vh', overflowY: 'auto' }} className="p-4">
                    <div className="terms-content">
                        <div dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(cookiePolicy.content) }} />
                        <div className="mt-3 text-muted small">
                            {t('cookieNotice.version')}: {cookiePolicy.version} | {t('cookieNotice.effectiveDate')}:{' '}
                            {new Date(cookiePolicy.effectiveDate).toLocaleDateString()}
                        </div>
                    </div>
                </Modal.Body>
                <Modal.Footer className="border-top">
                    <Button variant="light" onClick={() => setShowModal(false)}>
                        {t('cookieNotice.close')}
                    </Button>
                    <Button variant="primary" onClick={dismissNotice}>
                        {t('cookieNotice.dismiss')}
                    </Button>
                </Modal.Footer>
            </Modal>
        </>
    )
}

export default CookieNotice
