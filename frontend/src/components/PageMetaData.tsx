import type { FC } from 'react'
import { Helmet } from 'react-helmet-async'
import { useBranding } from '@/context/BrandingContext'

type PageMetaDataProps = {
    title: string
    description?: string
    keywords?: string
}

const defaultPageDescription =
    'Eskineria is a modern, responsive admin dashboard. Ideal for building CRM, CMS, project management tools, and custom web applications.'
const defaultPageKeywords =
    'Eskineria, admin dashboard, responsive admin, CRM dashboard, CMS admin, web app UI'

const PageMetaData: FC<PageMetaDataProps> = ({
    title,
    description = defaultPageDescription,
    keywords = defaultPageKeywords
}) => {
    const { applicationName } = useBranding()
    const baseTitle = applicationName || 'Eskineria Admin'

    return (
        <Helmet>
            <title>{title ? `${title} | ${baseTitle}` : baseTitle}</title>
            <meta name="description" content={description} />
            <meta name="keywords" content={keywords} />
        </Helmet>
    )
}

export default PageMetaData
