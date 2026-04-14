
import { useEffect, useState } from 'react'
import { useLayoutContext } from '@/context/useLayoutContext'
import { Dropdown, DropdownItem, DropdownMenu, DropdownToggle } from 'react-bootstrap'
import { LuMonitorCog, LuMoon, LuSun } from 'react-icons/lu'
import type { IconType } from 'react-icons'
import type { LayoutThemeType } from '@/types/layout'
import clsx from 'clsx'
import { useTranslation } from 'react-i18next'

type ThemeType = {
    layoutTheme: LayoutThemeType
    icon: IconType
}

const ThemeToggler = () => {
    const { t } = useTranslation()
    const { theme, changeTheme } = useLayoutContext()
    const [mounted, setMounted] = useState(false)

    useEffect(() => {
        setMounted(true)
    }, [])

    if (!mounted) return null

    const themeOptions: ThemeType[] = [
        { layoutTheme: 'light', icon: LuSun },
        { layoutTheme: 'dark', icon: LuMoon },
        { layoutTheme: 'system', icon: LuMonitorCog },
    ]

    const labels: Record<LayoutThemeType, string> = {
        light: 'customizer.colors.light',
        dark: 'customizer.colors.dark',
        system: 'customizer.colors.system',
    }

    // Pick toggle icon based on current theme
    const ActiveIcon = theme === 'dark' ? LuMoon : theme === 'system' ? LuMonitorCog : LuSun

    return (
        <div className="topbar-item mx-1">
            <Dropdown >
                <DropdownToggle as="button" className="topbar-link drop-arrow-none p-0 border-0 bg-transparent">
                    <ActiveIcon className="fs-xxl" />
                </DropdownToggle>

                <DropdownMenu align={'end'} className="dropdown-menu-end thememode-dropdown shadow-sm border-0">
                    {themeOptions.map(({ layoutTheme, icon: Icon }) => (
                        <li key={layoutTheme}>
                            <DropdownItem as={'label'} className={clsx('d-flex align-items-center cursor-pointer', { active: theme === layoutTheme })}>
                                <Icon className="me-2 fs-16" />
                                <span className="flex-grow-1">{t(labels[layoutTheme])}</span>
                                <input
                                    type="radio"
                                    className="form-check-input ms-auto"
                                    name="theme"
                                    value={layoutTheme}
                                    checked={theme === layoutTheme}
                                    onChange={() => changeTheme(layoutTheme)}
                                />
                            </DropdownItem>
                        </li>
                    ))}
                </DropdownMenu>
            </Dropdown>
        </div>
    )
}

export default ThemeToggler
