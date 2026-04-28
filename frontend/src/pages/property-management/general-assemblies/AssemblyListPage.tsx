import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import { Container, Card } from 'react-bootstrap'

const AssemblyListPage = () => {
    return (
        <VerticalLayout>
            <PageBreadcrumb title="Genel Kurul İşlemleri" subtitle="Genel Kurullar & Hazirun Cetveli" />
            <Container fluid>
                <Card className="border-0 shadow-sm p-4 text-center">
                    <h4 className="text-muted">Bu sayfa "A'dan Z'ye" kodlama aşamasında yapılacaktır.</h4>
                    <p>Burada tüm genel kurulların listesi, yeni toplantı oluşturma ve hazirun cetveli PDF alma işlemleri yer alacak.</p>
                </Card>
            </Container>
        </VerticalLayout>
    )
}
export default AssemblyListPage
