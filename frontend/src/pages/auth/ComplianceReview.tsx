import DOMPurify from 'dompurify'
import { useEffect, useMemo, useState } from 'react'
import { Alert, Badge, Button, Form, Modal, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import AuthShell from './components/AuthShell'
import { useAuth } from '@/context/AuthContext'
import { AuthService } from '@/services/authService'
import { ComplianceService } from '@/services/complianceService'
import type { TermsDto } from '@/types/compliance'
import { showToast } from '@/utils/toast'

const formatDate = (value: string, locale: string) => {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleDateString(locale)
}

const getLocalizedValue = (record: Record<string, string> | null | undefined, culture: string) => {
  if (!record) return ''
  return record[culture] || record['en-US'] || record['tr-TR'] || Object.values(record).find(v => !!v) || ''
}

const Index = () => {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const { pendingRequiredTerms, refresh } = useAuth()

  const [acceptedIds, setAcceptedIds] = useState<string[]>([])
  const [selectedTerm, setSelectedTerm] = useState<TermsDto | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isLoggingOut, setIsLoggingOut] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  const pendingIdsKey = useMemo(
    () => pendingRequiredTerms.map((term) => term.id).sort().join('|'),
    [pendingRequiredTerms],
  )

  useEffect(() => {
    setAcceptedIds([])
    setErrorMessage('')
  }, [pendingIdsKey])

  const getTypeLabel = (type: string) => {
    if (type === 'TermsOfService') {
      return t('auth.complianceReview.types.termsOfService')
    }

    if (type === 'PrivacyPolicy') {
      return t('auth.complianceReview.types.privacyPolicy')
    }

    return type
  }

  const allAccepted = pendingRequiredTerms.length > 0 && pendingRequiredTerms.every((term) => acceptedIds.includes(term.id))

  const toggleAccepted = (id: string) => {
    setAcceptedIds((prev) => {
      if (prev.includes(id)) {
        return prev.filter((currentId) => currentId !== id)
      }

      return [...prev, id]
    })
    setErrorMessage('')
  }

  const handleSubmit = async () => {
    if (pendingRequiredTerms.length === 0) {
      navigate('/', { replace: true })
      return
    }

    if (!allAccepted) {
      setErrorMessage(t('auth.complianceReview.validation'))
      return
    }

    setIsSubmitting(true)
    setErrorMessage('')

    try {
      for (const term of pendingRequiredTerms) {
        const response = await ComplianceService.acceptTerms({
          termsAndConditionsId: term.id,
        })

        if (!response.success) {
          setErrorMessage(response.message || t('auth.complianceReview.acceptError'))
          return
        }
      }

      await refresh()
      showToast(t('auth.complianceReview.success'), 'success')
      navigate('/', { replace: true })
    } catch {
      // Global axios interceptor handles errors.
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleLogout = async () => {
    setIsLoggingOut(true)
    try {
      await AuthService.logout()
    } catch {
      // Logout should still continue even if the API call fails.
    } finally {
      navigate('/auth/login', { replace: true })
      setIsLoggingOut(false)
    }
  }

  return (
    <AuthShell
      footer={(
        <p className="text-muted text-center mt-4 mb-0">
          <button
            type="button"
            className="btn btn-link p-0 text-decoration-underline link-offset-3 fw-semibold"
            onClick={() => void handleLogout()}
            disabled={isLoggingOut || isSubmitting}
          >
            {isLoggingOut ? t('common.loading') : t('auth.complianceReview.logout')}
          </button>
        </p>
      )}
    >
      <div className="text-center">
        <h3>{t('auth.complianceReview.title')}</h3>
        <p className="text-muted auth-sub-text mx-auto mb-0">
          {t('auth.complianceReview.subtitle')}
        </p>
      </div>

      <Alert variant="warning" className="mt-4">
        {t('auth.complianceReview.notice')}
      </Alert>

      {errorMessage ? (
        <Alert variant="danger" className="mt-3">
          {errorMessage}
        </Alert>
      ) : null}

      {pendingRequiredTerms.length === 0 ? (
        <div className="d-flex align-items-center justify-content-center gap-2 text-muted mt-4">
          <Spinner animation="border" size="sm" />
          <span>{t('common.loading')}</span>
        </div>
      ) : (
        <div className="mt-4 d-flex flex-column gap-3">
          {pendingRequiredTerms.map((term) => (
            <div key={term.id} className="border rounded-3 p-3 bg-light-subtle">
              <div className="d-flex flex-wrap align-items-center gap-2 mb-2">
                <Badge bg="primary">{getTypeLabel(term.type)}</Badge>
                <span className="text-muted small">v{term.version}</span>
                <span className="text-muted small">
                  {t('auth.complianceReview.effectiveDate')}: {formatDate(term.effectiveDate, i18n.language)}
                </span>
              </div>

              <h6 className="mb-2">{getLocalizedValue(term.summary, i18n.language) || getTypeLabel(term.type)}</h6>

              <div className="d-flex flex-wrap align-items-center justify-content-between gap-2">
                <Form.Check
                  id={`accept-terms-${term.id}`}
                  checked={acceptedIds.includes(term.id)}
                  onChange={() => toggleAccepted(term.id)}
                  label={t('auth.complianceReview.acceptLabel', {
                    document: getTypeLabel(term.type),
                    version: term.version,
                  })}
                />

                <Button
                  variant="link"
                  className="p-0 text-decoration-underline"
                  onClick={() => setSelectedTerm(term)}
                >
                  {t('auth.complianceReview.viewDocument')}
                </Button>
              </div>
            </div>
          ))}

          <Button
            variant="primary"
            className="w-100 mt-2"
            onClick={() => void handleSubmit()}
            disabled={isSubmitting}
          >
            {isSubmitting ? t('common.loading') : t('auth.complianceReview.submit')}
          </Button>
        </div>
      )}

      <Modal show={selectedTerm !== null} onHide={() => setSelectedTerm(null)} centered size="lg">
        <Modal.Header closeButton>
          <Modal.Title>{selectedTerm ? getTypeLabel(selectedTerm.type) : ''}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {selectedTerm ? (
            <>
              <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
                <Badge bg="primary">{getTypeLabel(selectedTerm.type)}</Badge>
                <span className="text-muted small">v{selectedTerm.version}</span>
                <span className="text-muted small">
                  {t('auth.complianceReview.effectiveDate')}: {formatDate(selectedTerm.effectiveDate, i18n.language)}
                </span>
              </div>

              <h5>{getLocalizedValue(selectedTerm.summary, i18n.language) || getTypeLabel(selectedTerm.type)}</h5>

              <div
                className="border rounded p-3 bg-light-subtle mt-3"
                style={{ maxHeight: 420, overflowY: 'auto' }}
                dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(getLocalizedValue(selectedTerm.content, i18n.language)) }}
              />
            </>
          ) : null}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="light" onClick={() => setSelectedTerm(null)}>
            {t('common.close')}
          </Button>
        </Modal.Footer>
      </Modal>
    </AuthShell>
  )
}

export default Index
