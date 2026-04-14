
import {Link} from "react-router-dom";

import logoDark from '@/assets/images/logo-black.png'
import logo from '@/assets/images/logo.png'
import { useBranding } from '@/context/BrandingContext'

const AppLogo = ({ height }: { height?: number }) => {
  const { applicationLogoUrl, applicationName } = useBranding()
  const logoSource = applicationLogoUrl || logo
  const logoDarkSource = applicationLogoUrl || logoDark

  return (
    <>
      <Link to="/" className="logo-dark">
        <img src={logoDarkSource} alt={applicationName} height={height ?? 28} />
      </Link>
      <Link to="/" className="logo-light">
        <img src={logoSource} alt={applicationName} height={height ?? 28} />
      </Link>
    </>
  )
}

export default AppLogo
