import { createContext, useCallback, useContext, useEffect, useMemo, useState, type PropsWithChildren } from 'react'
import SystemSettingsService from '@/services/systemSettingsService'
import type { AuthSystemSettings } from '@/types/systemSettings'

type BrandingState = {
    applicationName: string
    applicationLogoUrl: string | null
    applicationFaviconUrl: string | null
}

type BrandingContextValue = BrandingState & {
    refreshBranding: () => Promise<void>
    applyBranding: (next: Partial<BrandingState>) => void
}

const DEFAULT_BRANDING: BrandingState = {
    applicationName: 'Eskineria Backend',
    applicationLogoUrl: null,
    applicationFaviconUrl: null,
}

const BrandingContext = createContext<BrandingContextValue | null>(null)
const defaultFaviconHref =
    (typeof document !== 'undefined'
        ? (document.querySelector('link[rel="icon"]') as HTMLLinkElement | null)?.href
        : null) || '/vite.svg'

const toBrandingState = (settings?: AuthSystemSettings | null): BrandingState => {
    const appName = settings?.applicationName?.trim() || DEFAULT_BRANDING.applicationName
    const logo = settings?.applicationLogoUrl?.trim() || null
    const favicon = settings?.applicationFaviconUrl?.trim() || null

    return {
        applicationName: appName,
        applicationLogoUrl: logo,
        applicationFaviconUrl: favicon,
    }
}

const updateFavicon = (url: string | null) => {
    const head = document.head
    let iconLink = document.querySelector('link[rel="icon"]') as HTMLLinkElement | null
    if (!iconLink) {
        iconLink = document.createElement('link')
        iconLink.rel = 'icon'
        head.appendChild(iconLink)
    }

    iconLink.href = url || defaultFaviconHref
}

export const BrandingProvider = ({ children }: PropsWithChildren) => {
    const [branding, setBranding] = useState<BrandingState>(DEFAULT_BRANDING)

    const refreshBranding = useCallback(async () => {
        try {
            const response = await SystemSettingsService.getPublicAuthSettings()
            if (response.success && response.data) {
                setBranding(toBrandingState(response.data))
            }
        } catch {
            // Global axios interceptor handles toast when necessary.
        }
    }, [])

    const applyBranding = useCallback((next: Partial<BrandingState>) => {
        setBranding((prev) => ({
            ...prev,
            ...next,
            applicationName: next.applicationName?.trim() || prev.applicationName,
            applicationLogoUrl: next.applicationLogoUrl === undefined ? prev.applicationLogoUrl : next.applicationLogoUrl,
            applicationFaviconUrl: next.applicationFaviconUrl === undefined ? prev.applicationFaviconUrl : next.applicationFaviconUrl,
        }))
    }, [])

    useEffect(() => {
        void refreshBranding()
    }, [refreshBranding])

    useEffect(() => {
        updateFavicon(branding.applicationFaviconUrl)
    }, [branding.applicationFaviconUrl])

    const contextValue = useMemo<BrandingContextValue>(() => ({
        applicationName: branding.applicationName,
        applicationLogoUrl: branding.applicationLogoUrl,
        applicationFaviconUrl: branding.applicationFaviconUrl,
        refreshBranding,
        applyBranding,
    }), [applyBranding, branding.applicationFaviconUrl, branding.applicationLogoUrl, branding.applicationName, refreshBranding])

    return (
        <BrandingContext.Provider value={contextValue}>
            {children}
        </BrandingContext.Provider>
    )
}

export const useBranding = () => {
    const context = useContext(BrandingContext)
    if (!context) {
        return {
            ...DEFAULT_BRANDING,
            refreshBranding: async () => { },
            applyBranding: () => { },
        }
    }

    return context
}
