import { Container } from 'react-bootstrap'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import { useTranslation } from 'react-i18next'
import { useEffect } from 'react'
import { showToast } from '@/utils/toast'

const Dashboard = () => {
    const { t } = useTranslation()

    useEffect(() => {
        const loginSuccessToast = sessionStorage.getItem('loginSuccessToast')
        const pendingToast = sessionStorage.getItem('roleSwitchSuccessToast')

        const timerId = window.setTimeout(() => {
            if (loginSuccessToast) {
                showToast(loginSuccessToast, 'success')
                sessionStorage.removeItem('loginSuccessToast')
            }

            if (pendingToast) {
                showToast(pendingToast, 'success')
                sessionStorage.removeItem('roleSwitchSuccessToast')
            }
        }, 120)

        return () => window.clearTimeout(timerId)
    }, [])

    return (
        <VerticalLayout>
            <Container fluid>
                <PageBreadcrumb title={t('common.blank_page')} />

                <div className="row">
                    <div className="col-12">
                        {/* Start content here */}
                    </div>
                </div>
            </Container>
        </VerticalLayout>
    )
}

export default Dashboard
