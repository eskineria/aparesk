import { useLayoutContext } from '@/context/useLayoutContext'
import type {
    LayoutOrientationType,
    LayoutPositionType,
    LayoutSkinType,
    LayoutThemeType,
    SideNavType,
    TopBarType
} from '@/types/layout'

import { Fragment } from 'react'
import { Button, Col, Offcanvas, Row } from 'react-bootstrap'
import { TbX } from 'react-icons/tb'
import { useTranslation } from 'react-i18next'

import pattern from '@/assets/images/user-bg-pattern.png'

import classicImg from '@/assets/images/layouts/themes/theme-default.png'
import flatImg from '@/assets/images/layouts/themes/theme-flat.png'
import materialImg from '@/assets/images/layouts/themes/theme-material.png'
import minimalImg from '@/assets/images/layouts/themes/theme-minimal.png'
import modernImg from '@/assets/images/layouts/themes/theme-modern.png'
import saasImg from '@/assets/images/layouts/themes/theme-saas.png'

import dark from '@/assets/images/layouts/dark.svg'
import { default as light, default as lightSideNavImg } from '@/assets/images/layouts/light.svg'
import system from '@/assets/images/layouts/system.svg'

import darkSideNavImg from '@/assets/images/layouts/side-dark.svg'
import gradientSideNavImg from '@/assets/images/layouts/side-gradient.svg'
import graySideNavImg from '@/assets/images/layouts/side-gray.svg'
import imageSideNavImg from '@/assets/images/layouts/side-image.svg'
import darkTopBarImg from '@/assets/images/layouts/topbar-dark.svg'
import gradientTopBarImg from '@/assets/images/layouts/topbar-gradient.svg'
import grayTopBarImg from '@/assets/images/layouts/topbar-gray.svg'
import lightTopBarImg from '@/assets/images/layouts/topbar-light.svg'

import compactSideNavImg from '@/assets/images/layouts/sidebar-compact.svg'
import smallSideNavImg from '@/assets/images/layouts/sidebar-sm.svg'
import fullSideNavImg from '@/assets/images/layouts/sidebar-full.svg'
import SimpleBar from "simplebar-react";

type SkinOptionType = {
    skin: LayoutSkinType
    image: string
    disabled?: boolean
    labelKey: string
}

type ThemeOptionType = {
    theme: LayoutThemeType
    image: string
    labelKey: string
}

type OrientationOptionType = {
    orientation: LayoutOrientationType
    image: string
    labelKey: string
}

type TopBarColorOptionType = {
    color: TopBarType['color']
    image: string
    labelKey: string
}

type SideNavColorOptionType = {
    color: SideNavType['color']
    image: string
    labelKey: string
}

type SideNavSizeOptionType = {
    labelKey: string
    size: SideNavType['size']
    image: string
}

const skinOptions: SkinOptionType[] = [
    { skin: 'default', image: classicImg, labelKey: 'customizer.themes.default' },
    { skin: 'material', image: materialImg, labelKey: 'customizer.themes.material' },
    { skin: 'modern', image: modernImg, labelKey: 'customizer.themes.modern' },
    { skin: 'saas', image: saasImg, labelKey: 'customizer.themes.saas' },
    { skin: 'flat', image: flatImg, labelKey: 'customizer.themes.flat' },
    { skin: 'minimal', image: minimalImg, labelKey: 'customizer.themes.minimal' },
]

const themeOptions: ThemeOptionType[] = [
    { theme: 'light', image: light, labelKey: 'customizer.colors.light' },
    { theme: 'dark', image: dark, labelKey: 'customizer.colors.dark' },
    { theme: 'system', image: system, labelKey: 'customizer.colors.system' },
]

const orientationOptions: OrientationOptionType[] = [
    { orientation: 'vertical', image: darkSideNavImg, labelKey: 'customizer.orientations.vertical' },
    { orientation: 'horizontal', image: fullSideNavImg, labelKey: 'customizer.orientations.horizontal' },
]

const topBarColorOptions: TopBarColorOptionType[] = [
    { color: 'light', image: lightTopBarImg, labelKey: 'customizer.colors.light' },
    { color: 'dark', image: darkTopBarImg, labelKey: 'customizer.colors.dark' },
    { color: 'gray', image: grayTopBarImg, labelKey: 'customizer.colors.gray' },
    { color: 'gradient', image: gradientTopBarImg, labelKey: 'customizer.colors.gradient' },
]

const sidenavColorOptions: SideNavColorOptionType[] = [
    { color: 'light', image: lightSideNavImg, labelKey: 'customizer.colors.light' },
    { color: 'dark', image: darkSideNavImg, labelKey: 'customizer.colors.dark' },
    { color: 'gray', image: graySideNavImg, labelKey: 'customizer.colors.gray' },
    { color: 'gradient', image: gradientSideNavImg, labelKey: 'customizer.colors.gradient' },
    { color: 'image', image: imageSideNavImg, labelKey: 'customizer.colors.system' }, // Using system for image key temporarily or add new
]

const sidenavSizeOptions: SideNavSizeOptionType[] = [
    { size: 'default', image: lightSideNavImg, labelKey: 'customizer.sizes.default' },
    { size: 'compact', image: compactSideNavImg, labelKey: 'customizer.sizes.compact' },
    { size: 'condensed', image: smallSideNavImg, labelKey: 'customizer.sizes.condensed' },
    { size: 'on-hover', image: smallSideNavImg, labelKey: 'customizer.sizes.on_hover' },
    { size: 'on-hover-active', image: lightSideNavImg, labelKey: 'customizer.sizes.on_hover_active' },
    { size: 'offcanvas', image: fullSideNavImg, labelKey: 'customizer.sizes.offcanvas' },
]

const layoutPositionOptions: { position: LayoutPositionType; labelKey: string }[] = [
    { position: 'fixed', labelKey: 'customizer.positions.fixed' },
    { position: 'scrollable', labelKey: 'customizer.positions.scrollable' }
]

const Customizer = () => {
    const { t } = useTranslation()
    const {
        customizer,
        skin,
        changeSkin,
        theme,
        changeTheme,
        orientation,
        changeOrientation,
        topBar,
        changeTopBarColor,
        sidenav,
        changeSideNavColor,
        changeSideNavSize,
        position,
        changeLayoutPosition,
        toggleSideNavUser,
        reset,
    } = useLayoutContext()

    return (
        <Offcanvas show={customizer.isOpen} onHide={customizer.toggle} placement="end" className="overflow-hidden shadow-lg border-0">
            <div className="d-flex justify-content-between text-bg-primary gap-2 p-3" style={{ backgroundImage: `url(${pattern})`, backgroundSize: 'cover' }}>
                <div>
                    <h5 className="mb-1 fw-bold text-white text-uppercase">{t('customizer.title')}</h5>
                    <p className="text-white text-opacity-75 fst-italic fw-medium mb-0 small">
                        {t('customizer.subtitle')}
                    </p>
                </div>

                <div className="flex-grow-0">
                    <button
                        onClick={customizer.toggle}
                        type="button"
                        className="d-block btn btn-sm bg-white bg-opacity-25 text-white rounded-circle btn-icon border-0">
                        <TbX className="fs-lg" />
                    </button>
                </div>
            </div>

            <SimpleBar className="offcanvas-body p-0 h-100">
                <div className="p-3 border-bottom border-dashed">
                    <h5 className="mb-3 fw-bold small text-uppercase text-muted">{t('customizer.select_theme')}</h5>
                    <Row className="g-3">
                        {skinOptions.map((item, idx) => (
                            <Col sm={6} key={idx}>
                                <div className="form-check card-radio">
                                    <input
                                        id={`skin-${item.skin}`}
                                        className="form-check-input"
                                        type="radio"
                                        name="data-skin"
                                        disabled={item.disabled ?? false}
                                        value={item.skin}
                                        checked={skin === item.skin}
                                        onChange={() => changeSkin(item.skin)}
                                    />
                                    <label className="form-check-label p-0 w-100 cursor-pointer" htmlFor={`skin-${item.skin}`}>
                                        <img src={item.image} alt="" className="img-fluid rounded shadow-sm" width={167.5} height={111.66} />
                                    </label>
                                </div>
                                <h5 className="text-center text-muted mt-2 mb-0 small">{t(item.labelKey)}</h5>
                            </Col>
                        ))}
                    </Row>
                </div>

                <div className="p-3 border-bottom border-dashed">
                    <h5 className="mb-3 fw-bold small text-uppercase text-muted">{t('customizer.color_scheme')}</h5>
                    <Row className="g-3">
                        {themeOptions.map((item, idx) => (
                            <Col sm={4} key={idx}>
                                <div className="form-check card-radio">
                                    <input
                                        id={`theme-${item.theme}`}
                                        className="form-check-input"
                                        type="radio"
                                        name="data-bs-theme"
                                        value={item.theme}
                                        checked={theme === item.theme}
                                        onChange={() => changeTheme(item.theme)}
                                    />
                                    <label className="form-check-label p-0 w-100 cursor-pointer" htmlFor={`theme-${item.theme}`}>
                                        <img src={item.image} alt="" className="img-fluid rounded shadow-sm" width={104.33} height={83.45} />
                                    </label>
                                </div>
                                <h5 className="text-center text-muted mt-2 mb-0 small">{t(item.labelKey)}</h5>
                            </Col>
                        ))}
                    </Row>
                </div>

                <div className="p-3 border-bottom border-dashed">
                    <h5 className="mb-3 fw-bold small text-uppercase text-muted">{t('customizer.topbar_color')}</h5>
                    <Row className="g-3">
                        {topBarColorOptions.map((item, idx) => (
                            <Col sm={4} key={idx}>
                                <div className="form-check card-radio">
                                    <input
                                        id={`topbar-color-${item.color}`}
                                        className="form-check-input"
                                        type="radio"
                                        name="data-topbar-color"
                                        value={item.color}
                                        checked={topBar.color === item.color}
                                        onChange={() => changeTopBarColor(item.color)}
                                    />
                                    <label className="form-check-label p-0 w-100 cursor-pointer" htmlFor={`topbar-color-${item.color}`}>
                                        <img src={item.image} alt="" className="img-fluid rounded shadow-sm" width={104.33} height={83.45} />
                                    </label>
                                </div>
                                <h5 className="text-center text-muted mt-2 mb-0 small">{t(item.labelKey)}</h5>
                            </Col>
                        ))}
                    </Row>
                </div>

                <div className="p-3 border-bottom border-dashed">
                    <h5 className="mb-3 fw-bold small text-uppercase text-muted">{t('customizer.orientation')}</h5>
                    <Row className="g-3">
                        {orientationOptions.map((item, idx) => (
                            <Col sm={4} key={idx}>
                                <div className="form-check card-radio">
                                    <input
                                        id={`layout-${item.orientation}`}
                                        className="form-check-input"
                                        type="radio"
                                        name="data-layout"
                                        value={item.orientation}
                                        checked={orientation === item.orientation}
                                        onChange={() => changeOrientation(item.orientation)}
                                    />
                                    <label className="form-check-label p-0 w-100 cursor-pointer" htmlFor={`layout-${item.orientation}`}>
                                        <img src={item.image} alt="" className="img-fluid rounded shadow-sm" width={104.33} height={83.45} />
                                    </label>
                                </div>
                                <h5 className="text-center text-muted mt-2 mb-0 small">{t(item.labelKey)}</h5>
                            </Col>
                        ))}
                    </Row>
                </div>

                <div className="p-3 border-bottom border-dashed">
                    <h5 className="mb-3 fw-bold small text-uppercase text-muted">{t('customizer.sidenav_color')}</h5>
                    <Row className="g-3">
                        {sidenavColorOptions.map((item, idx) => (
                            <Col sm={4} key={idx}>
                                <div className="form-check card-radio">
                                    <input
                                        id={`sidenav-color-${item.color}`}
                                        className="form-check-input"
                                        type="radio"
                                        name="data-menu-color"
                                        value={item.color}
                                        checked={sidenav.color === item.color}
                                        onChange={() => changeSideNavColor(item.color)}
                                    />
                                    <label className="form-check-label p-0 w-100 cursor-pointer" htmlFor={`sidenav-color-${item.color}`}>
                                        <img src={item.image} alt="" className="img-fluid rounded shadow-sm" width={104.33} height={83.45} />
                                    </label>
                                </div>
                                <h5 className="text-center text-muted mt-2 mb-0 small">{t(item.labelKey)}</h5>
                            </Col>
                        ))}
                    </Row>
                </div>

                {orientation !== 'horizontal' && (
                    <>
                        <div className="p-3 border-bottom border-dashed">
                            <h5 className="mb-3 fw-bold small text-uppercase text-muted">{t('customizer.sidebar_size')}</h5>
                            <Row className="g-3">
                                {sidenavSizeOptions.map((item, idx) => (
                                    <Col sm={4} key={idx}>
                                        <div className="form-check card-radio">
                                            <input
                                                id={`sidenav-size-${item.size}`}
                                                className="form-check-input"
                                                type="radio"
                                                name="data-sidenav-size"
                                                value={item.size}
                                                checked={sidenav.size === item.size}
                                                onChange={() => changeSideNavSize(item.size)}
                                            />
                                            <label className="form-check-label p-0 w-100 cursor-pointer" htmlFor={`sidenav-size-${item.size}`}>
                                                <img src={item.image} alt="" className="img-fluid rounded shadow-sm" width={104.33} height={83.45} />
                                            </label>
                                        </div>
                                        <h5 className="text-center text-muted mt-2 mb-0 small">{t(item.labelKey)}</h5>
                                    </Col>
                                ))}
                            </Row>
                        </div>

                        <div className="p-3 border-bottom border-dashed">
                            <div className="d-flex justify-content-between align-items-center">
                                <h5 className="fw-bold mb-0 small text-uppercase text-muted">{t('customizer.layout_position')}</h5>

                                <div className="btn-group radio" role="group">
                                    {layoutPositionOptions.map((item, idx) => (
                                        <Fragment key={idx}>
                                            <input
                                                type="radio"
                                                className="btn-check"
                                                name="data-layout-position"
                                                id={`position-${item.position}`}
                                                value={item.position}
                                                checked={position === item.position}
                                                onChange={() => changeLayoutPosition(item.position)}
                                            />
                                            <label className="btn btn-sm btn-soft-warning w-sm" htmlFor={`position-${item.position}`}>
                                                {t(item.labelKey)}
                                            </label>
                                        </Fragment>
                                    ))}
                                </div>
                            </div>
                        </div>

                        <div className="p-3">
                            <div className="d-flex justify-content-between align-items-center">
                                <h5 className="mb-0 small text-uppercase text-muted">
                                    <label className="fw-bold m-0 cursor-pointer" htmlFor="sidebaruser-check">
                                        {t('customizer.sidebar_user')}
                                    </label>
                                </h5>

                                <div className="form-check form-switch fs-lg">
                                    <input type="checkbox" className="form-check-input cursor-pointer" id="sidebaruser-check" name="sidebar-user" checked={sidenav.user} onChange={toggleSideNavUser} />
                                </div>
                            </div>
                        </div>
                    </>
                )}
            </SimpleBar>

            <div className="offcanvas-footer border-top p-3 text-center bg-light">
                <Button variant="outline-secondary" type="button" onClick={reset} className="fw-semibold py-2 w-100 small">
                    {t('common.reset')}
                </Button>
            </div>
        </Offcanvas>
    )
}

export default Customizer
