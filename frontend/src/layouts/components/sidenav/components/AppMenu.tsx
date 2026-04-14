
import { useLayoutContext } from '@/context/useLayoutContext'
import { useAuth } from '@/context/AuthContext'
import { scrollToElement } from '@/helpers/layout'
import { menuItems } from '@/layouts/components/data'
import type { MenuItemType } from '@/types/layout'
import { Link, useLocation } from "react-router-dom";
import { useEffect, useMemo, useState } from 'react'
import { Collapse } from 'react-bootstrap'
import { TbChevronDown } from 'react-icons/tb'
import { useTranslation } from 'react-i18next'

const MenuItemWithChildren = ({
    item,
    openMenuKey,
    setOpenMenuKey,
    level = 0,
}: {
    item: MenuItemType
    openMenuKey: string | null
    setOpenMenuKey: (key: string | null) => void
    level?: number
}) => {
    const { t } = useTranslation()
    const { pathname } = useLocation()
    const isTopLevel = level === 0

    const [localOpen, setLocalOpen] = useState(false)
    const [didAutoOpen, setDidAutoOpen] = useState(false)

    const isChildActive = (children: MenuItemType[]): boolean =>
        children.some((child) => (child.url && pathname.endsWith(child.url)) || (child.children && isChildActive(child.children)))

    const isActive = isChildActive(item.children || [])

    const isOpen = isTopLevel ? openMenuKey === item.key : localOpen

    useEffect(() => {
        if (isTopLevel && isActive && !didAutoOpen) {
            setOpenMenuKey(item.key)
            setDidAutoOpen(true)
        }
        if (!isTopLevel && isActive && !didAutoOpen) {
            setLocalOpen(true)
            setDidAutoOpen(true)
        }
    }, [isActive, isTopLevel, item.key, setOpenMenuKey, didAutoOpen])

    const toggleOpen = () => {
        if (isTopLevel) {
            setOpenMenuKey(isOpen ? null : item.key)
        } else {
            setLocalOpen((prev) => !prev)
        }
    }

    return (
        <li className={`side-nav-item ${isOpen ? 'active' : ''}`}>
            <button onClick={toggleOpen} className="side-nav-link" aria-expanded={isOpen}>
                {item.icon && (
                    <span className="menu-icon">
                        <item.icon />
                    </span>
                )}
                <span className="menu-text">{item.labelKey ? t(item.labelKey) : item.label}</span>
                {item.badge ? (
                    <span className={`badge bg-${item.badge.variant}`}>{item.badge.text}</span>
                ) : (
                    <TbChevronDown className="menu-arrow" />
                )}
            </button>
            <Collapse in={isOpen}>
                <div>
                    <ul className="sub-menu">
                        {(item.children || []).map((child) =>
                            child.children ? (
                                <MenuItemWithChildren key={child.key} item={child} openMenuKey={openMenuKey} setOpenMenuKey={setOpenMenuKey} level={level + 1} />
                            ) : (
                                <MenuItem key={child.key} item={child} />
                            ),
                        )}
                    </ul>
                </div>
            </Collapse>
        </li>
    )
}

const MenuItem = ({ item }: { item: MenuItemType }) => {
    const { t } = useTranslation()
    const { pathname } = useLocation()
    const isActive = item.url && (pathname === item.url || pathname.endsWith(item.url))

    const { sidenav, hideBackdrop } = useLayoutContext()

    const toggleBackdrop = () => {
        if (sidenav.size === 'offcanvas') {
            hideBackdrop()
        }
    }

    return (
        <li className={`side-nav-item ${isActive ? 'active' : ''}`}>
            <Link
                to={item.url ?? '/'}
                onClick={toggleBackdrop}
                className={`side-nav-link  ${isActive ? 'active' : ''} ${item.isDisabled ? 'disabled' : ''} ${item.isSpecial ? 'special-menu' : ''}`}>
                {item.icon && (
                    <span className="menu-icon">
                        <item.icon />
                    </span>
                )}
                <span className="menu-text">{item.labelKey ? t(item.labelKey) : item.label}</span>
                {item.badge && <span className={`badge text-bg-${item.badge.variant} opacity-50`}>{item.badge.text}</span>}
            </Link>
        </li>
    )
}

const AppMenu = () => {
    const { t } = useTranslation()
    const { permissions, activeRole } = useAuth()
    const [openMenuKey, setOpenMenuKey] = useState<string | null>(null)

    const hasPermission = (permission: string) => {
        return permissions.includes(permission)
    }

    const canView = (item: MenuItemType) => {
        // Role check (if item specifies required roles)
        if (item.roles && item.roles.length > 0) {
            if (!activeRole || !item.roles.includes(activeRole)) return false
        }

        // Permission check
        if (!item.permission) return true
        return hasPermission(item.permission)
    }

    const filterMenu = (items: MenuItemType[]): MenuItemType[] => {
        const filtered = items
            .map((item) => {
                if (item.children && item.children.length > 0) {
                    const filteredChildren = filterMenu(item.children)
                    const isAllowed = canView(item)
                    const canShowParent = item.isTitle || Boolean(item.url) || filteredChildren.length > 0
                    if (!isAllowed || !canShowParent) return null
                    return { ...item, children: filteredChildren }
                }

                return canView(item) ? item : null
            })
            .filter((item): item is MenuItemType => item !== null)

        // Hide section titles when the section has no visible entries.
        return filtered.filter((item, index, arr) => {
            if (!item.isTitle) {
                return true
            }

            for (let i = index + 1; i < arr.length; i += 1) {
                if (arr[i].isTitle) {
                    return false
                }

                return true
            }

            return false
        })
    }

    const visibleMenuItems = useMemo(() => filterMenu(menuItems), [permissions, activeRole])

    const scrollToActiveLink = () => {
        const activeItem: HTMLElement | null = document.querySelector('.side-nav-link.active')
        if (activeItem) {
            const simpleBarContent = document.querySelector('#sidenav .simplebar-content-wrapper')
            if (simpleBarContent) {
                const offset = activeItem.offsetTop - window.innerHeight * 0.4
                scrollToElement(simpleBarContent, offset, 500)
            }
        }
    }

    useEffect(() => {
        setTimeout(() => scrollToActiveLink(), 100)
    }, [])

    return (
        <ul className="side-nav">
            {visibleMenuItems.map((item) =>
                item.isTitle ? (
                    <li className={'side-nav-title mt-2'} key={item.key}>
                        {item.labelKey ? t(item.labelKey) : item.label}
                    </li>
                ) : item.children ? (
                    <MenuItemWithChildren key={item.key} item={item} openMenuKey={openMenuKey} setOpenMenuKey={setOpenMenuKey} />
                ) : (
                    <MenuItem key={item.key} item={item} />
                ),
            )}
        </ul>
    )
}

export default AppMenu
