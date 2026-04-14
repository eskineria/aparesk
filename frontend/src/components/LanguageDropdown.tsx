import { useEffect, useState } from 'react'
import { Dropdown } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import localizationService from '@/services/localizationService'
import flagTR from '@/assets/images/flags/tr.svg'
import flagUS from '@/assets/images/flags/us.svg'
import flagES from '@/assets/images/flags/es.svg'
import flagFR from '@/assets/images/flags/fr.svg'
import flagDE from '@/assets/images/flags/de.svg'
import flagGeneric from '@/assets/images/flags/xx.svg'

type LanguageOption = {
  code: string
  name: string
  flag: string
}

const languageMap: Record<string, Omit<LanguageOption, 'code'>> = {
  'en-US': { name: 'English', flag: flagUS },
  'tr-TR': { name: 'Türkçe', flag: flagTR },
  'es-ES': { name: 'Español', flag: flagES },
  'fr-FR': { name: 'Français', flag: flagFR },
  'de-DE': { name: 'Deutsch', flag: flagDE },
}

const fallbackLanguages: LanguageOption[] = [
  { code: 'en-US', name: 'English', flag: flagUS },
  { code: 'tr-TR', name: 'Türkçe', flag: flagTR },
]

const toLanguageOption = (code: string): LanguageOption => ({
  code,
  name: languageMap[code]?.name || code,
  flag: languageMap[code]?.flag || flagGeneric,
})

const findLanguage = (languages: LanguageOption[], currentCode: string): LanguageOption | undefined => {
  const exact = languages.find((lang) => lang.code === currentCode)
  if (exact) return exact

  const currentPrefix = currentCode.split('-')[0]?.toLowerCase()
  return languages.find((lang) => lang.code.split('-')[0]?.toLowerCase() === currentPrefix)
}

const LanguageDropdown = () => {
  const { i18n } = useTranslation()
  const [languages, setLanguages] = useState<LanguageOption[]>(fallbackLanguages)

  useEffect(() => {
    let isMounted = true

    const fetchLanguages = async () => {
      try {
        const cultureCodes = await localizationService.getCultures()
        if (!isMounted) return

        if (!cultureCodes.length) {
          setLanguages(fallbackLanguages)
          return
        }

        const available = [...new Set(cultureCodes)].map(toLanguageOption)
        setLanguages(available)

        const current = findLanguage(available, i18n.language)
        if (!current) {
          const defaultLang = available.find((lang) => lang.code === 'en-US') || available[0]
          if (defaultLang) await i18n.changeLanguage(defaultLang.code)
          return
        }

        if (current.code !== i18n.language) {
          await i18n.changeLanguage(current.code)
        }
      } catch {
        if (!isMounted) return
        setLanguages(fallbackLanguages)
      }
    }

    void fetchLanguages()

    const handleCulturesUpdated = () => {
      void fetchLanguages()
    }

    window.addEventListener('localization:cultures-updated', handleCulturesUpdated)
    return () => {
      isMounted = false
      window.removeEventListener('localization:cultures-updated', handleCulturesUpdated)
    }
  }, [i18n])

  const selectedLang = findLanguage(languages, i18n.language) || languages[0]

  if (!selectedLang) return null

  return (
    <Dropdown>
      <Dropdown.Toggle as="a" className="nav-link cursor-pointer d-flex align-items-center gap-1" style={{ cursor: 'pointer', textDecoration: 'none' }}>
        <img src={selectedLang.flag} alt={selectedLang.name} height="14" className="me-0" />
        <span className="align-middle text-muted text-uppercase fw-semibold fs-13">{selectedLang.code.split('-')[0]}</span>
      </Dropdown.Toggle>
      <Dropdown.Menu className="dropdown-menu-end">
        {languages.map((lang) => (
          <Dropdown.Item
            key={lang.code}
            onClick={() => void i18n.changeLanguage(lang.code)}
            active={selectedLang.code === lang.code}
          >
            <img src={lang.flag} alt={lang.name} height="12" className="me-2" />
            <span className="align-middle">{lang.name}</span>
          </Dropdown.Item>
        ))}
      </Dropdown.Menu>
    </Dropdown>
  )
}

export default LanguageDropdown
