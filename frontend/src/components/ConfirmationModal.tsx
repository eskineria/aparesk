import type { ReactNode } from 'react'
import { Button, Modal, ModalBody, ModalFooter, ModalHeader, ModalTitle } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'

type ConfirmationModalProps = {
    show: boolean
    onHide: () => void
    onConfirm: () => void
    title?: string
    message?: string | ReactNode
    confirmText?: string
    cancelText?: string
    variant?: string
    isLoading?: boolean
}

const ConfirmationModal = ({
    show,
    onHide,
    onConfirm,
    title,
    message,
    confirmText,
    cancelText,
    variant = 'primary',
    isLoading = false
}: ConfirmationModalProps) => {
    const { t } = useTranslation()
    const resolvedTitle = title ?? t('common.confirmAction')
    const resolvedMessage = message ?? t('common.confirmProceed')
    const resolvedConfirmText = confirmText ?? t('common.confirm')
    const resolvedCancelText = cancelText ?? t('common.cancel')

    return (
        <Modal show={show} onHide={onHide} centered>
            <ModalHeader closeButton className="bg-light">
                <ModalTitle className="fs-16">{resolvedTitle}</ModalTitle>
            </ModalHeader>
            <ModalBody className="py-4 text-center">
                <div className="mb-3">
                    <i className={`ti ti-help fs-48 text-${variant}`}></i>
                </div>
                <div className="fs-15 fw-medium">{resolvedMessage}</div>
            </ModalBody>
            <ModalFooter className="bg-light bg-opacity-50">
                <Button variant="light" onClick={onHide} disabled={isLoading}>
                    {resolvedCancelText}
                </Button>
                <Button variant={variant} onClick={onConfirm} disabled={isLoading}>
                    {isLoading ? (
                        <>
                            <span className="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
                            {resolvedConfirmText}...
                        </>
                    ) : (
                        resolvedConfirmText
                    )}
                </Button>
            </ModalFooter>
        </Modal>
    )
}

export default ConfirmationModal
