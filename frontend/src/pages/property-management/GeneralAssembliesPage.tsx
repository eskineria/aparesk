import { Col, Container, Row, Card } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuUserPlus, LuShield, LuBookOpen } from 'react-icons/lu'
import { TbGavel } from 'react-icons/tb'
import { useNavigate } from 'react-router-dom'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'

const GeneralAssembliesDashboard = () => {
    const { t } = useTranslation()
    const navigate = useNavigate()

    const cards = [
        {
            title: 'Genel Kurullar & Hazirun Cetveli',
            description: 'Genel kurul tanımlamaları, tarih ve dönem belirleme ile hazirun listesi.',
            icon: <TbGavel size={40} className="text-primary mb-3" />,
            color: 'primary',
            path: '/management/properties/general-assemblies/list'
        },
        {
            title: 'Yönetim Kurulu Belirleme',
            description: 'Asil ve yedek yönetim kurulu üyelerini atama işlemleri.',
            icon: <LuUserPlus size={40} className="text-success mb-3" />,
            color: 'success',
            path: '/management/properties/general-assemblies/management-board'
        },
        {
            title: 'Denetim Kurulu Belirleme',
            description: 'Asil ve yedek denetim kurulu üyelerini atama işlemleri.',
            icon: <LuShield size={40} className="text-warning mb-3" />,
            color: 'warning',
            path: '/management/properties/general-assemblies/audit-board'
        },
        {
            title: 'Karar Defteri',
            description: 'Genel kurul toplantısında alınan kararların yönetimi.',
            icon: <LuBookOpen size={40} className="text-info mb-3" />,
            color: 'info',
            path: '/management/properties/general-assemblies/decisions'
        }
    ]

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('propertyManagement.title')} subtitle="Genel Kurul İşlemleri" />
            <Container fluid>
                <div className="mb-4">
                    <h2 className="fw-bold text-dark">Genel Kurul İşlemleri</h2>
                    <p className="text-muted">Lütfen yapmak istediğiniz işlemi seçiniz.</p>
                </div>
                
                <Row className="g-4">
                    {cards.map((c, idx) => (
                        <Col md={6} xl={3} key={idx}>
                            <Card 
                                className={`h-100 border-0 shadow-sm rounded-4 overflow-hidden card-menu cursor-pointer`}
                                onClick={() => navigate(c.path)}
                            >
                                <Card.Body className="p-4 text-center d-flex flex-column align-items-center justify-content-center">
                                    <div className={`icon-wrapper bg-${c.color} bg-opacity-10 rounded-circle p-4 mb-3`}>
                                        {c.icon}
                                    </div>
                                    <h5 className="fw-bold mb-2">{c.title}</h5>
                                    <p className="text-muted fs-sm mb-0">{c.description}</p>
                                </Card.Body>
                            </Card>
                        </Col>
                    ))}
                </Row>
            </Container>

            <style>{`
                .cursor-pointer { cursor: pointer; }
                .card-menu { transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1); border-top: 4px solid transparent; }
                .card-menu:hover { 
                    transform: translateY(-8px); 
                    box-shadow: 0 15px 30px rgba(0,0,0,0.1) !important; 
                }
                .card-menu:hover .icon-wrapper {
                    transform: scale(1.1);
                    transition: transform 0.3s ease;
                }
            `}</style>
        </VerticalLayout>
    )
}

export default GeneralAssembliesDashboard
