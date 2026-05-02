import { useCallback, useEffect, useState } from 'react'
import { Button, Card, Col, Container, Form, Row, Table, CardHeader } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPlus, LuTrash2, LuBookOpen, LuMapPin, LuCalendar, LuPrinter, LuInfo } from 'react-icons/lu'
import { showToast } from '@/utils/toast'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import PropertyManagementService from '@/services/propertyManagementService'
import type { SiteListItem, GeneralAssemblyListItem, GeneralAssemblyDecision } from '@/types/propertyManagement'

const DecisionsPage = () => {
    const { t } = useTranslation()
    const [sites, setSites] = useState<SiteListItem[]>([])
    const [assemblies, setAssemblies] = useState<GeneralAssemblyListItem[]>([])
    
    const [selectedSiteId, setSelectedSiteId] = useState('')
    const [selectedAssemblyId, setSelectedAssemblyId] = useState('')
    const [assemblyDetail, setAssemblyDetail] = useState<any>(null)

    useEffect(() => {
        const fetchSites = async () => {
            const result = await PropertyManagementService.getSites({ pageNumber: 1, pageSize: 100 })
            setSites(result.items)
        }
        fetchSites()
    }, [])

    useEffect(() => {
        if (selectedSiteId) {
            const fetchAssemblies = async () => {
                const result = await PropertyManagementService.getGeneralAssemblies({ siteId: selectedSiteId, pageNumber: 1, pageSize: 100 })
                setAssemblies(result.items)
            }
            fetchAssemblies()
        }
    }, [selectedSiteId])

    const fetchDetail = useCallback(async () => {
        if (!selectedAssemblyId) return
        try {
            const data = await PropertyManagementService.getGeneralAssembly(selectedAssemblyId)
            setAssemblyDetail(data)
        } catch (error) {
            showToast(t('common.error'), 'danger')
        }
    }, [selectedAssemblyId, t])

    useEffect(() => {
        fetchDetail()
    }, [fetchDetail])

    const addDecision = () => {
        if (!assemblyDetail) return
        const nextNumber = (assemblyDetail.decisions?.length || 0) + 1
        const newDecision: GeneralAssemblyDecision = {
            decisionNumber: nextNumber,
            description: ''
        }
        setAssemblyDetail({ ...assemblyDetail, decisions: [...(assemblyDetail.decisions || []), newDecision] })
    }

    const updateDecision = (idx: number, field: keyof GeneralAssemblyDecision, value: any) => {
        const newDecisions = [...assemblyDetail.decisions]
        newDecisions[idx] = { ...newDecisions[idx], [field]: value }
        setAssemblyDetail({ ...assemblyDetail, decisions: newDecisions })
    }

    const removeDecision = (idx: number) => {
        const newDecisions = assemblyDetail.decisions.filter((_: any, i: number) => i !== idx)
        // Re-order decision numbers
        const reordered = newDecisions.map((d: any, i: number) => ({ ...d, decisionNumber: i + 1 }))
        setAssemblyDetail({ ...assemblyDetail, decisions: reordered })
    }

    const save = async () => {
        try {
            await PropertyManagementService.updateGeneralAssembly(selectedAssemblyId, assemblyDetail)
            showToast(t('common.success'), 'success')
        } catch (error) {
            showToast(t('common.error'), 'danger')
        }
    }

    const printDecisions = () => {
        window.print()
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title="Genel Kurul İşlemleri" subtitle="Karar Defteri" />
            <Container fluid>
                <Card className="border-0 shadow-sm mb-4 overflow-hidden print-hide">
                    <Card.Body className="p-4 bg-gradient-info-soft">
                        <Row className="g-3 align-items-end">
                            <Col md={5}>
                                <Form.Group>
                                    <Form.Label className="fw-bold text-dark mb-2">
                                        <LuMapPin className="me-1 text-info" /> Site Seçimi
                                    </Form.Label>
                                    <Form.Select 
                                        className="form-select-lg border-0 shadow-sm"
                                        value={selectedSiteId} 
                                        onChange={(e) => setSelectedSiteId(e.target.value)}
                                    >
                                        <option value="">Lütfen site seçiniz...</option>
                                        {sites.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                                    </Form.Select>
                                </Form.Group>
                            </Col>
                            <Col md={5}>
                                <Form.Group>
                                    <Form.Label className="fw-bold text-dark mb-2">
                                        <LuCalendar className="me-1 text-info" /> Genel Kurul Dönemi
                                    </Form.Label>
                                    <Form.Select 
                                        className="form-select-lg border-0 shadow-sm"
                                        value={selectedAssemblyId} 
                                        onChange={(e) => setSelectedAssemblyId(e.target.value)}
                                        disabled={!selectedSiteId}
                                    >
                                        <option value="">Lütfen genel kurul seçiniz...</option>
                                        {assemblies.map(a => <option key={a.id} value={a.id}>{a.term} - {new Date(a.meetingDate).toLocaleDateString('tr-TR')}</option>)}
                                    </Form.Select>
                                </Form.Group>
                            </Col>
                            <Col md={2}>
                                <Button 
                                    variant="info" 
                                    className="w-100 btn-lg shadow-sm text-white"
                                    disabled={!selectedAssemblyId}
                                    onClick={fetchDetail}
                                >
                                    Listele
                                </Button>
                            </Col>
                        </Row>
                    </Card.Body>
                </Card>

                {assemblyDetail ? (
                    <Card className="border-0 shadow-sm rounded-4 overflow-hidden mb-4">
                        <CardHeader className="bg-white border-bottom-0 pt-4 px-4 d-flex align-items-center justify-content-between flex-wrap gap-3">
                            <div>
                                <h4 className="mb-1 fw-bold d-flex align-items-center gap-2">
                                    <LuBookOpen className="text-info" /> Alınan Kararlar
                                </h4>
                                <p className="text-muted small mb-0">
                                    {assemblyDetail.siteName} - {assemblyDetail.term} Dönemi
                                </p>
                            </div>
                            <div className="d-flex gap-2 print-hide">
                                <Button variant="outline-secondary" size="sm" onClick={printDecisions} className="d-flex align-items-center gap-1">
                                    <LuPrinter /> Yazdır
                                </Button>
                                <Button variant="soft-info" size="sm" onClick={addDecision} className="d-flex align-items-center gap-1">
                                    <LuPlus /> Karar Ekle
                                </Button>
                                <Button variant="info" size="sm" onClick={save} className="px-3 shadow-sm text-white">
                                    Kararları Kaydet
                                </Button>
                            </div>
                        </CardHeader>
                        <Card.Body className="p-0">
                            <div className="table-responsive">
                                <Table hover className="table-centered mb-0 align-middle">
                                    <thead className="bg-light bg-opacity-50">
                                        <tr>
                                            <th className="ps-4 py-3" style={{ width: '80px' }}>No</th>
                                            <th style={{ width: '85%' }}>Karar Açıklaması</th>
                                            <th className="text-end pe-4 print-hide">İşlem</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {(!assemblyDetail.decisions || assemblyDetail.decisions.length === 0) ? (
                                            <tr>
                                                <td colSpan={3} className="text-center py-5">
                                                    <div className="py-4">
                                                        <LuBookOpen size={48} className="text-muted mb-3 opacity-25" />
                                                        <h5 className="text-muted">Henüz karar girişi yapılmamış.</h5>
                                                        <p className="text-muted small">Üstteki butonu kullanarak yeni karar ekleyebilirsiniz.</p>
                                                    </div>
                                                </td>
                                            </tr>
                                        ) : assemblyDetail.decisions.map((decision: any, idx: number) => (
                                            <tr key={idx}>
                                                <td className="ps-4">
                                                    <div className="avatar-sm bg-soft-info text-info rounded-circle d-flex align-items-center justify-content-center fw-bold">
                                                        {decision.decisionNumber}
                                                    </div>
                                                </td>
                                                <td className="py-3">
                                                    <Form.Control 
                                                        as="textarea"
                                                        rows={3}
                                                        className="bg-light border-0 decision-textarea"
                                                        placeholder="Karar metnini giriniz..."
                                                        value={decision.description}
                                                        onChange={(e) => updateDecision(idx, 'description', e.target.value)}
                                                    />
                                                </td>
                                                <td className="text-end pe-4 print-hide">
                                                    <Button variant="link" className="text-danger p-0" onClick={() => removeDecision(idx)}>
                                                        <LuTrash2 />
                                                    </Button>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </Table>
                            </div>
                        </Card.Body>
                        <Card.Footer className="bg-white border-top-0 p-4 text-center text-muted small">
                            <LuInfo className="me-1" /> All decisions taken at the general assembly must be recorded in the official decision book.
                        </Card.Footer>
                    </Card>
                ) : (
                    <div className="text-center py-5 bg-white rounded-4 shadow-sm">
                        <LuBookOpen size={64} className="text-info mb-3 opacity-25" />
                        <h4 className="text-dark fw-bold">Karar Defteri Yönetimi</h4>
                        <p className="text-muted mx-auto" style={{ maxWidth: '400px' }}>
                            Alınan kararları görüntülemek ve düzenlemek için lütfen yukarıdan bir site ve genel kurul dönemi seçiniz.
                        </p>
                    </div>
                )}
            </Container>

            <style>{`
                .bg-gradient-info-soft {
                    background: linear-gradient(135deg, rgba(var(--bs-info-rgb), 0.05) 0%, rgba(var(--bs-info-rgb), 0.1) 100%);
                }
                .form-select-lg {
                    font-size: 1rem;
                    padding-top: 0.75rem;
                    padding-bottom: 0.75rem;
                }
                @media print {
                    .print-hide { display: none !important; }
                    .card { border: none !important; box-shadow: none !important; }
                    body { background: white !important; }
                    .container-fluid { padding: 0 !important; }
                    .decision-textarea { border: none !important; background: transparent !important; resize: none !important; overflow: visible !important; }
                }
                .table-centered td { vertical-align: top; }
                .decision-textarea:focus {
                    background-color: white !important;
                    border: 1px solid var(--bs-info) !important;
                    box-shadow: 0 0 0 0.25rem rgba(var(--bs-info-rgb), 0.1);
                }
            `}</style>
        </VerticalLayout>
    )
}

export default DecisionsPage
