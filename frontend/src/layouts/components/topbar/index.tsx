
import { useLayoutContext } from '@/context/useLayoutContext'
import CustomizerToggler from '@/layouts/components/topbar/components/CustomizerToggler'
import LanguageDropdown from '@/layouts/components/topbar/components/LanguageDropdown'
import MessageDropdown from '@/layouts/components/topbar/components/MessageDropdown'
import ThemeToggler from '@/layouts/components/topbar/components/ThemeToggler'
import UserProfile from '@/layouts/components/topbar/components/UserProfile'
import RoleSwitcher from '@/layouts/components/topbar/components/RoleSwitcher'

import { Link } from "react-router-dom";
import { Container } from 'react-bootstrap'
import { TbMenu4 } from 'react-icons/tb'

import logoDark from '@/assets/images/logo-black.png'
import logoSm from '@/assets/images/logo-sm.png'
import logo from '@/assets/images/logo.png'
import FullscreenToggle from '@/layouts/components/topbar/components/FullscreenToggle'
import MonochromeThemeModeToggler from '@/layouts/components/topbar/components/MonochromeThemeModeToggler'
import { useBranding } from '@/context/BrandingContext'

const Topbar = () => {
    const { sidenav, changeSideNavSize, showBackdrop } = useLayoutContext()
    const { applicationLogoUrl, applicationName } = useBranding()
    const logoLargeSrc = applicationLogoUrl || logo
    const logoDarkLargeSrc = applicationLogoUrl || logoDark
    const logoSmallSrc = applicationLogoUrl || logoSm

    const toggleSideNav = () => {
        const html = document.documentElement
        const currentSize = html.getAttribute('data-sidenav-size')

        if (currentSize === 'offcanvas') {
            html.classList.toggle('sidebar-enable')
            showBackdrop()
        } else if (sidenav.size === 'compact') {
            changeSideNavSize(currentSize === 'compact' ? 'condensed' : 'compact', false)
        } else {
            changeSideNavSize(currentSize === 'condensed' ? 'default' : 'condensed')
        }
    }

    return (
        <header className="app-topbar shadow-sm">
            <Container fluid className="topbar-menu">
                <div className="d-flex align-items-center gap-2">
                    <div className="logo-topbar">
                        <Link to="/" className="logo-light">
                            <span className="logo-lg">
                                <img src={logoLargeSrc} alt={applicationName} height={28} />
                            </span>
                            <span className="logo-sm">
                                <img src={logoSmallSrc} alt={applicationName} height={28} />
                            </span>
                        </Link>

                        <Link to="/" className="logo-dark">
                            <span className="logo-lg">
                                <img src={logoDarkLargeSrc} alt={applicationName} height={28} />
                            </span>
                            <span className="logo-sm">
                                <img src={logoSmallSrc} alt={applicationName} height={28} />
                            </span>
                        </Link>
                    </div>

                    <button onClick={toggleSideNav} className="sidenav-toggle-button btn btn-default btn-icon border-0 bg-transparent">
                        <TbMenu4 size={24} className="text-white" />
                    </button>

                </div>

                <div className="d-flex align-items-center gap-2">
                    <LanguageDropdown />

                    <MessageDropdown />

                    <ThemeToggler />

                    <FullscreenToggle />

                    <MonochromeThemeModeToggler />

                    <RoleSwitcher />

                    <UserProfile />

                    <CustomizerToggler />
                </div>
            </Container>
        </header>
    )
}

export default Topbar
