import React, { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '@/context/AuthContext';
import { SystemSettingsService } from '@/services/systemSettingsService';

interface MaintenanceGuardProps {
    children: React.ReactNode;
}

const MaintenanceGuard: React.FC<MaintenanceGuardProps> = ({ children }) => {
    const { roles, isLoading: authLoading } = useAuth();
    const location = useLocation();
    const navigate = useNavigate();
    const [, setIsMaintenance] = useState<boolean | null>(null);

    useEffect(() => {
        const checkMaintenance = async () => {
            // Bypass maintenance check for public pages, error pages, and the maintenance page itself
            const isMaintenancePage = location.pathname === '/maintenance';
            const isAuthPage = location.pathname.startsWith('/auth/');
            const isCaptchaPage = location.pathname.startsWith('/auth/captcha');
            const isErrorPage = location.pathname.startsWith('/error/');
            const isPublicPage = ['/confirm-email', '/reset-password'].some(path => location.pathname.startsWith(path));

            if (isMaintenancePage || isCaptchaPage || isErrorPage || isPublicPage) return;

            try {
                const response = await SystemSettingsService.getPublicAuthSettings();
                if (response.success && response.data?.maintenanceModeEnabled) {
                    setIsMaintenance(true);

                    // If maintenance is on and user is NOT admin, redirect to maintenance page
                    // We allow admins to see the login page and the rest of the app
                    const isAdmin = roles.includes('Admin');
                    if (!isAdmin && !isAuthPage) {
                        navigate('/maintenance');
                    }
                } else {
                    setIsMaintenance(false);
                }
            } catch (error) {
                // If 503 happens here, axios interceptor will handle it
                console.error('Maintenance check failed', error);
            }
        };

        if (!authLoading) {
            checkMaintenance();
        }
    }, [location.pathname, roles, authLoading, navigate]);

    return <>{children}</>;
};

export default MaintenanceGuard;
