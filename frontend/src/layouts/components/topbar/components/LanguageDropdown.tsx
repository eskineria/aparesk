import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Dropdown, DropdownItem, DropdownMenu, DropdownToggle } from 'react-bootstrap'
import localizationService from '@/services/localizationService'

import flagTR from '@/assets/images/flags/tr.svg'
import flagUS from '@/assets/images/flags/us.svg'
import flagES from '@/assets/images/flags/es.svg'
import flagFR from '@/assets/images/flags/fr.svg'
import flagDE from '@/assets/images/flags/de.svg'
import flagGeneric from '@/assets/images/flags/xx.svg'

export type LanguageOptionType = {
    code: string
    name: string
    nativeName: string
    flag: string
}

const languageMap: Record<string, Partial<LanguageOptionType>> = {
    'en-US': { name: 'English', nativeName: 'English', flag: flagUS },
    'tr-TR': { name: 'Turkish', nativeName: 'Türkçe', flag: flagTR },
    'es-ES': { name: 'Spanish', nativeName: 'Español', flag: flagES },
    'fr-FR': { name: 'French', nativeName: 'Français', flag: flagFR },
    'de-DE': { name: 'German', nativeName: 'Deutsch', flag: flagDE },
}

const LanguageDropdown = () => {
    const { i18n } = useTranslation()
    const [availableLanguages, setAvailableLanguages] = useState<LanguageOptionType[]>([])

    useEffect(() => {
        const fetchLanguages = async () => {
            try {
                const codes = await localizationService.getCultures()
                const langs = codes.map(code => ({
                    code,
                    name: languageMap[code]?.name || code,
                    nativeName: languageMap[code]?.nativeName || code,
                    flag: languageMap[code]?.flag || flagGeneric
                }))
                setAvailableLanguages(langs)

                if (!langs.some(lang => lang.code === i18n.language)) {
                    const fallback = langs.find(lang => lang.code === 'en-US')?.code || langs[0]?.code || 'en-US'
                    await i18n.changeLanguage(fallback)
                }
            } catch (error) {
                console.error('Failed to fetch languages:', error)
                // Fallback to minimal set
                setAvailableLanguages([
                    { code: 'en-US', name: 'English', nativeName: 'English', flag: flagUS },
                    { code: 'tr-TR', name: 'Turkish', nativeName: 'Türkçe', flag: flagTR },
                ])
            }
        }

        fetchLanguages()

        const handleCulturesUpdated = () => {
            fetchLanguages()
        }

        window.addEventListener('localization:cultures-updated', handleCulturesUpdated)
        return () => window.removeEventListener('localization:cultures-updated', handleCulturesUpdated)
    }, [i18n])

    const currentLanguage = availableLanguages.find(lang => lang.code === i18n.language) ||
        availableLanguages.find(lang => lang.code === 'en-US') ||
        availableLanguages[0] ||
        { code: 'en-US', name: 'English', nativeName: 'English', flag: flagUS }

    const changeLanguage = (lang: LanguageOptionType) => {
        i18n.changeLanguage(lang.code)
    }

    if (availableLanguages.length === 0) return null

    return (
        <div className="topbar-item">
            <Dropdown align="end">
                <DropdownToggle as={'button'} className="topbar-link fw-bold drop-arrow-none border-0 bg-transparent">
                    <img src={currentLanguage.flag} alt={currentLanguage.name} className="me-0 rounded" width="22" height="16" />
                </DropdownToggle>
                <DropdownMenu className="dropdown-menu-end shadow-sm border-0">
                    {availableLanguages.map((lang) => (
                        <DropdownItem
                            key={lang.code}
                            active={i18n.language === lang.code}
                            onClick={() => changeLanguage(lang)}
                            className="d-flex align-items-center"
                        >
                            <img src={lang.flag} alt={lang.name} className="me-2 rounded" width="18" height="12" />
                            <span className="align-middle">{lang.nativeName}</span>
                        </DropdownItem>
                    ))}
                </DropdownMenu>
            </Dropdown>
        </div>
    )
}

export default LanguageDropdown
