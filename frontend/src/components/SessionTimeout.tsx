import React, { useState, useEffect, useCallback, useRef } from 'react'
import { Modal, Button } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { AuthService } from '@/services/authService'
import { useNavigate, useLocation } from 'react-router-dom'

interface SessionTimeoutProps {
    timeoutInMinutes?: number
    warningInMinutes?: number
}

const PUBLIC_PATHS = ['/auth/login', '/auth/register', '/auth/forgot-password', '/reset-password', '/confirm-email', '/error']

const SessionTimeout: React.FC<SessionTimeoutProps> = ({
    timeoutInMinutes = 15,
    warningInMinutes = 1
}) => {
    const { t } = useTranslation()
    const navigate = useNavigate()
    const location = useLocation()
    const [showModal, setShowModal] = useState(false)
    const [timeLeft, setTimeLeft] = useState(0)

    // Convert to milliseconds
    const timeoutMs = timeoutInMinutes * 60 * 1000
    const warningMs = warningInMinutes * 60 * 1000
    const warningThresholdMs = timeoutMs - warningMs

    const lastActivityRef = useRef<number>(Date.now())
    const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)
    const countdownIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

    const updateActivity = useCallback(() => {
        // Don't track activity on public pages
        if (PUBLIC_PATHS.some(path => location.pathname.includes(path))) return

        lastActivityRef.current = Date.now()
        // If modal is not shown, we can assume user is active and safe.
    }, [location.pathname])

    const checkSession = useCallback(() => {
        // Don't check session on public pages
        if (PUBLIC_PATHS.some(path => location.pathname.includes(path))) return

        const now = Date.now()
        const timeSinceLastActivity = now - lastActivityRef.current

        if (timeSinceLastActivity >= timeoutMs) {
            // Timeout reached
            logoutUser()
        } else if (timeSinceLastActivity >= warningThresholdMs && !showModal) {
            // Warning threshold reached
            setShowModal(true)
            setTimeLeft(Math.floor((timeoutMs - timeSinceLastActivity) / 1000))
        }
    }, [timeoutMs, warningThresholdMs, showModal, location.pathname])

    const logoutUser = async () => {
        try {
            await AuthService.logout()
        } catch (error) {
            console.error(error)
        } finally {
            setShowModal(false)
            if (timerRef.current) clearInterval(timerRef.current)
            if (countdownIntervalRef.current) clearInterval(countdownIntervalRef.current)
            navigate('/auth/login')
        }
    }

    const continueSession = async () => {
        try {
            // Call refresh token to extend session on backend
            await AuthService.refreshToken()

            // Reset local timer
            lastActivityRef.current = Date.now()
            setShowModal(false)
        } catch (error) {
            // If refresh fails, force logout
            logoutUser()
        }
    }

    useEffect(() => {
        const events = ['mousemove', 'keydown', 'wheel', 'mousedown', 'touchstart', 'scroll']

        const handleActivity = () => updateActivity()

        events.forEach(event => window.addEventListener(event, handleActivity))

        // Check every second
        timerRef.current = setInterval(checkSession, 1000)

        return () => {
            events.forEach(event => window.removeEventListener(event, handleActivity))
            if (timerRef.current) clearInterval(timerRef.current)
        }
    }, [updateActivity, checkSession])

    // Specific effect for the countdown inside the modal
    useEffect(() => {
        if (showModal) {
            countdownIntervalRef.current = setInterval(() => {
                const now = Date.now()
                const timeSinceLastActivity = now - lastActivityRef.current
                const remaining = Math.max(0, Math.ceil((timeoutMs - timeSinceLastActivity) / 1000))

                setTimeLeft(remaining)

                if (remaining <= 0) {
                    logoutUser()
                }
            }, 1000)
        } else {
            if (countdownIntervalRef.current) clearInterval(countdownIntervalRef.current)
        }

        return () => {
            if (countdownIntervalRef.current) clearInterval(countdownIntervalRef.current)
        }
    }, [showModal, timeoutMs])

    return (
        <Modal show={showModal} onHide={() => { }} backdrop="static" keyboard={false} centered>
            <Modal.Header>
                <Modal.Title className="text-danger">
                    <i className="mdi mdi-alert-circle-outline me-2"></i>
                    {t('auth.sessionTimeout.title')}
                </Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <p className="mb-0">
                    {t('auth.sessionTimeout.message', { seconds: timeLeft })}
                </p>
            </Modal.Body>
            <Modal.Footer>
                <Button variant="light" onClick={logoutUser}>
                    {t('auth.sessionTimeout.logout')}
                </Button>
                <Button variant="primary" onClick={continueSession}>
                    {t('auth.sessionTimeout.continue')}
                </Button>
            </Modal.Footer>
        </Modal>
    )
}

export default SessionTimeout
