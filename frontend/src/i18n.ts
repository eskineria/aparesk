import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import HttpBackend from 'i18next-http-backend';
import { trackMissingTranslationKey } from '@/services/missingTranslationTracker';

i18n
    .use(HttpBackend)
    .use(LanguageDetector)
    .use(initReactI18next)
    .init({
        backend: {
            loadPath: `${import.meta.env.VITE_API_BASE_URL}/api/v1/localization/resources?lang={{lng}}`,
        },
        fallbackLng: ['en-US', 'tr-TR'],
        debug: import.meta.env.DEV,

        interpolation: {
            escapeValue: false, // React escapes by default
        },
        parseMissingKeyHandler: (key) => {
            trackMissingTranslationKey(key)
            return key
        },
        react: {
            useSuspense: true,
        },
    });

export default i18n;
