import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useBranding } from '@/context/BrandingContext';
import AppLogo from '@/components/AppLogo';
import { currentYear } from '@/helpers';
import { Container, Row, Col, Card, CardBody } from 'react-bootstrap';
import maintenanceImg from '@/assets/images/maintenance.svg';
import SystemSettingsService from '@/services/systemSettingsService';

const Maintenance: React.FC = () => {
    const { t } = useTranslation();
    const { applicationName } = useBranding();
    const navigate = useNavigate();

    const [targetDate, setTargetDate] = useState<Date | null>(null);
    const [timeLeft, setTimeLeft] = useState<{ days: number, hours: number, minutes: number, seconds: number } | null>(null);
    const [maintenanceMessage, setMaintenanceMessage] = useState<string>('');
    const [countdownEnabled, setCountdownEnabled] = useState(true);
    const [isChecking, setIsChecking] = useState(false);

    const loadSettings = async () => {
        if (isChecking) return;
        setIsChecking(true);
        try {
            const response = await SystemSettingsService.getPublicAuthSettings();
            if (response.success && response.data) {
                // If maintenance mode is disabled, redirect away from this page
                if (!response.data.maintenanceModeEnabled) {
                    navigate('/dashboard');
                    return;
                }

                const countdown = response.data.maintenanceCountdownEnabled !== false;
                setCountdownEnabled(countdown);
                setMaintenanceMessage(response.data.maintenanceMessage?.trim() || '');

                if (countdown && response.data.maintenanceEndTime) {
                    const date = new Date(response.data.maintenanceEndTime);
                    if (!isNaN(date.getTime()) && date.getTime() > new Date().getTime()) {
                        setTargetDate(date);
                    } else {
                        // Date is in the past or invalid
                        setTargetDate(null);
                        setTimeLeft(null);
                    }
                } else {
                    setTargetDate(null);
                    setTimeLeft(null);
                }
            }
        } catch (error) {
            console.error("Failed to load maintenance settings", error);
        } finally {
            setIsChecking(false);
        }
    };

    useEffect(() => {
        let isMounted = true;

        // Initial load
        const initialLoad = async () => {
            const response = await SystemSettingsService.getPublicAuthSettings();
            if (!isMounted) return;
            if (response.success && response.data) {
                if (!response.data.maintenanceModeEnabled) {
                    navigate('/dashboard');
                    return;
                }
                const countdown = response.data.maintenanceCountdownEnabled !== false;
                setCountdownEnabled(countdown);
                setMaintenanceMessage(response.data.maintenanceMessage?.trim() || '');

                if (countdown && response.data.maintenanceEndTime) {
                    const date = new Date(response.data.maintenanceEndTime);
                    if (!isNaN(date.getTime()) && date.getTime() > new Date().getTime()) {
                        setTargetDate(date);
                    }
                } else {
                    setTargetDate(null);
                    setTimeLeft(null);
                }
            }
        };

        void initialLoad();
        return () => { isMounted = false; };
    }, [navigate]);

    useEffect(() => {
        if (!targetDate) return;

        const calculateTimeLeft = () => {
            const now = new Date();
            const difference = targetDate.getTime() - now.getTime();

            if (difference > 0) {
                return {
                    days: Math.floor(difference / (1000 * 60 * 60 * 24)),
                    hours: Math.floor((difference / (1000 * 60 * 60)) % 24),
                    minutes: Math.floor((difference / 1000 / 60) % 60),
                    seconds: Math.floor((difference / 1000) % 60),
                };
            }
            return null;
        };

        const timer = setInterval(() => {
            const newTimeLeft = calculateTimeLeft();
            if (!newTimeLeft) {
                clearInterval(timer);
                setTargetDate(null);
                setTimeLeft(null);
                // Instead of reload, just check settings once more
                void loadSettings();
            } else {
                setTimeLeft(newTimeLeft);
            }
        }, 1000);

        setTimeLeft(calculateTimeLeft());

        return () => clearInterval(timer);
    }, [targetDate]);

    return (
        <div className="auth-box d-flex align-items-center justify-content-center min-vh-100 position-relative overflow-hidden" style={{ background: 'var(--bs-light)' }}>
            {/* Background SVG Decoration */}
            <div className="position-absolute top-0 end-0 d-none d-sm-block" style={{ width: 400, zIndex: 0, opacity: 0.1 }}>
                <svg width="100%" height="auto" viewBox="0 0 600 560" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M377.923 101.019L315.106 163.836C299.517 179.425 274.242 179.425 258.653 163.836L195.836 101.019C180.247 85.4301 180.247 60.1551 195.836 44.5661L258.653 -18.251C274.242 -33.84 299.517 -33.84 315.106 -18.251L377.923 44.5661C393.512 60.1551 393.512 85.4301 377.923 101.019Z" stroke="var(--bs-primary)" strokeWidth="2" strokeMiterlimit="10" />
                    <path d="M403.998 311.555L372.211 343.342C361.79 353.763 344.894 353.763 334.473 343.342L302.686 311.555C292.265 301.134 292.265 284.238 302.686 273.817L334.473 242.03C344.894 231.609 361.79 231.609 372.211 242.03L403.998 273.817C414.419 284.238 414.419 301.134 403.998 311.555Z" fill="var(--bs-primary)" />
                    <path d="M417.828 85.0572L355.011 147.874C339.422 163.463 314.147 163.463 298.558 147.874L235.741 85.0572C220.152 69.4682 220.152 44.1931 235.741 28.6051L298.558 -34.2119C314.147 -49.8009 339.422 -49.8009 355.011 -34.2119L417.828 28.6051C433.417 44.1931 433.417 69.4682 417.828 85.0572Z" fill="var(--bs-info)" />
                </svg>
            </div>

            <Container style={{ zIndex: 1 }}>
                <Row className="justify-content-center">
                    <Col xl={5} lg={7} md={9}>
                        <Card className="shadow-lg border-0 overflow-hidden" style={{ borderRadius: '1.25rem' }}>
                            <CardBody className="p-4 p-md-5">
                                <div className="text-center mb-0">
                                    <div className="mb-4">
                                        <AppLogo height={42} />
                                    </div>
                                    <div className="mx-auto mb-4" style={{ maxWidth: '240px' }}>
                                        <img src={maintenanceImg} alt="Maintenance" className="img-fluid floating-animation" />
                                    </div>
                                    <h3 className="fw-bold text-uppercase mt-0 mb-3" style={{ letterSpacing: '2px', color: 'var(--bs-dark)' }}>
                                        {t('error.maintenance.title', 'We\'ll Be Back Soon!')}
                                    </h3>
                                    <p className="text-muted mb-4 lead font-15">
                                        {maintenanceMessage || t('error.maintenance.description', 'Our system is currently undergoing scheduled maintenance to improve your experience.')}
                                    </p>

                                    {countdownEnabled && (
                                        <div className="countdown-container py-3 mb-2">
                                            {timeLeft ? (
                                                <div className="d-flex justify-content-center gap-2 gap-sm-3">
                                                    {[
                                                        { label: t('common.days', 'Days'), value: timeLeft.days },
                                                        { label: t('common.hours', 'Hours'), value: timeLeft.hours },
                                                        { label: t('common.minutes', 'Minutes'), value: timeLeft.minutes },
                                                        { label: t('common.seconds', 'Seconds'), value: timeLeft.seconds }
                                                    ].map((item, index) => (
                                                        <React.Fragment key={index}>
                                                            <div className="countdown-box text-center">
                                                                <div className="display-6 fw-bold text-primary">{String(item.value).padStart(2, '0')}</div>
                                                                <div className="text-muted small text-uppercase fw-semibold" style={{ fontSize: '0.65rem' }}>{item.label}</div>
                                                            </div>
                                                            {index < 3 && <div className="display-6 fw-bold text-muted opacity-25">:</div>}
                                                        </React.Fragment>
                                                    ))}
                                                </div>
                                            ) : (
                                                <div className="bg-light p-3 rounded-pill">
                                                    <p className="text-primary fw-semibold mb-0">
                                                        <i className="ri-time-line me-1"></i>
                                                        {t('error.maintenance.returning_soon', 'We are returning right now...')}
                                                    </p>
                                                </div>
                                            )}
                                        </div>
                                    )}
                                </div>

                                <p className="text-center text-muted mt-5 mb-0 small opacity-75">
                                    © {currentYear} {applicationName}. {t('error.maintenance.stay_tuned', 'Stay tuned.')}
                                </p>
                            </CardBody>
                        </Card>
                    </Col>
                </Row>
            </Container>

            <style>{`
                .floating-animation {
                    animation: float 4s ease-in-out infinite;
                }
                @keyframes float {
                    0% { transform: translateY(0px); }
                    50% { transform: translateY(-12px); }
                    100% { transform: translateY(0px); }
                }
                .countdown-box {
                    min-width: 60px;
                }
                [data-bs-theme="dark"] .bg-light {
                    background-color: rgba(255,255,255,0.05) !important;
                }
            `}</style>
        </div>
    );
};

export default Maintenance;
