import Customizer from '@/layouts/components/customizer'
import Footer from '@/layouts/components/footer'
import Sidenav from '@/layouts/components/sidenav'
import Topbar from '@/layouts/components/topbar'
import MissingTranslationBanner from '@/components/MissingTranslationBanner'
import { Fragment } from 'react'

import type { ChildrenType } from '@/types/common'

type VerticalLayoutProps = {
    hideMissingTranslationBanner?: boolean
} & ChildrenType

const VerticalLayout = ({ children, hideMissingTranslationBanner }: VerticalLayoutProps) => {
    return (
        <Fragment>
            <div className="wrapper">
                <Sidenav />

                <Topbar />

                <div className="content-page">
                    {!hideMissingTranslationBanner && <MissingTranslationBanner />}

                    {children}

                    <Footer />
                </div>
            </div>

            <Customizer />
        </Fragment>
    )
}

export default VerticalLayout
