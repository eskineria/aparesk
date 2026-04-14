import { useEffect, useMemo, useState } from 'react'
import { Alert, Button } from 'react-bootstrap'
import { useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import localizationService from '@/services/localizationService'
import { useAuth } from '@/context/AuthContext'
import {
    clearMissingTranslationKeys,
    subscribeMissingTranslationKeys,
} from '@/services/missingTranslationTracker'

const MAX_PREVIEW_KEYS = 12

const MissingTranslationBanner = () => {
    const { t } = useTranslation()
    const location = useLocation()
    const { permissions, isLoading: isAuthLoading } = useAuth()
    const [enabled, setEnabled] = useState(false)
    const [isLoading, setIsLoading] = useState(true)
    const [missingKeys, setMissingKeys] = useState<string[]>([])
    const canManageLocalization = permissions.includes('Localization.Manage')

    useEffect(() => {
        if (isAuthLoading) {
            return
        }

        if (!canManageLocalization) {
            setEnabled(false)
            setIsLoading(false)
            return
        }

        let mounted = true
        const loadCapabilities = async () => {
            try {
                const capabilities = await localizationService.getCapabilities()
                if (mounted) {
                    setEnabled(Boolean(capabilities.missingTranslationBannerEnabled))
                }
            } catch {
                if (mounted) {
                    setEnabled(false)
                }
            } finally {
                if (mounted) {
                    setIsLoading(false)
                }
            }
        }

        void loadCapabilities()
        return () => {
            mounted = false
        }
    }, [canManageLocalization, isAuthLoading])

    useEffect(() => {
        return subscribeMissingTranslationKeys((keys) => {
            setMissingKeys(keys)
        })
    }, [])

    useEffect(() => {
        clearMissingTranslationKeys()
    }, [location.pathname])

    const previewKeys = useMemo(() => missingKeys.slice(0, MAX_PREVIEW_KEYS), [missingKeys])

    if (isLoading || !enabled || missingKeys.length === 0) {
        return null
    }

    return (
        <Alert variant="warning" className="m-3 mb-2 py-2 d-flex flex-column gap-2">
            <div className="d-flex align-items-center justify-content-between gap-2">
                <div className="fw-semibold">
                    {t('identity.localization.missing_banner_title', { count: missingKeys.length })}
                </div>
                <Button
                    variant="outline-dark"
                    size="sm"
                    onClick={() => clearMissingTranslationKeys()}>
                    {t('identity.localization.missing_banner_clear')}
                </Button>
            </div>
            <div className="small text-muted">
                {t('identity.localization.missing_banner_desc')}
            </div>
            <div className="small">
                {previewKeys.join(', ')}
                {missingKeys.length > previewKeys.length ? ` (+${missingKeys.length - previewKeys.length})` : ''}
            </div>
        </Alert>
    )
}

export default MissingTranslationBanner
