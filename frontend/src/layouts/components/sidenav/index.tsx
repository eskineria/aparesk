
import logoDark from '@/assets/images/logo-black.png'
import logoSm from '@/assets/images/logo-sm.png'
import logo from '@/assets/images/logo.png'
import { useBranding } from '@/context/BrandingContext'
import { useLayoutContext } from '@/context/useLayoutContext'
import AppMenu from '@/layouts/components/sidenav/components/AppMenu'
import UserProfile from '@/layouts/components/sidenav/components/UserProfile'

import { Link } from "react-router-dom";
import { TbMenu4, TbX } from 'react-icons/tb'
import SimpleBar from "simplebar-react";

const Sidenav = () => {
    const { sidenav, hideBackdrop, changeSideNavSize } = useLayoutContext()
    const { applicationLogoUrl, applicationName } = useBranding()
    const logoLargeSrc = applicationLogoUrl || logo
    const logoDarkLargeSrc = applicationLogoUrl || logoDark
    const logoSmallSrc = applicationLogoUrl || logoSm

    const toggleSidebar = () => {
        changeSideNavSize(sidenav.size === 'on-hover-active' ? 'on-hover' : 'on-hover-active')
    }

    const closeSidebar = () => {
        const html = document.documentElement
        html.classList.toggle('sidebar-enable')
        hideBackdrop()
    }

    return (
        <div className="sidenav-menu">
            <Link to="/" className="logo">
                <span className="logo logo-light">
                    <span className="logo-lg">
                        <img src={logoLargeSrc} alt={applicationName} />
                    </span>
                    <span className="logo-sm">
                        <img src={logoSmallSrc} alt={applicationName} />
                    </span>
                </span>

                <span className="logo logo-dark">
                    <span className="logo-lg">
                        <img src={logoDarkLargeSrc} alt={applicationName} />
                    </span>
                    <span className="logo-sm">
                        <img src={logoSmallSrc} alt={applicationName} />
                    </span>
                </span>
            </Link>

            <button className="button-on-hover">
                <TbMenu4 onClick={toggleSidebar} className="fs-22 align-middle text-white" />
            </button>

            <button className="button-close-offcanvas">
                <TbX onClick={closeSidebar} className="align-middle" />
            </button>

            <SimpleBar id="sidenav" className="scrollbar">
                {sidenav.user && <UserProfile />}
                <AppMenu />
            </SimpleBar>
        </div>
    )
}

export default Sidenav
