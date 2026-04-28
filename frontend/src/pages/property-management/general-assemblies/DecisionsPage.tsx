import { useCallback, useEffect, useState } from 'react'
import { Button, Card, Col, Container, Form, Row, Table, CardHeader } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPlus, LuTrash2, LuBookOpen } from 'react-icons/lu'
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

    useEffect(() => {
        if (selectedAssemblyId) {
            const fetchDetail = async () => {
                try {
                    const data = await PropertyManagementService.getGeneralAssembly(selectedAssemblyId)
                    setAssemblyDetail(data)
                } catch (error) {
                    showToast(t('common.error'), 'danger')
                }
            }
            fetchDetail()
        } else {
            setAssemblyDetail(null)
        }
    }, [selectedAssemblyId, t])

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

    return (
        <VerticalLayout>
            <PageBreadcrumb title="Genel Kurul İşlemleri" subtitle="Karar Defteri" />
            <Container fluid>
                <Card className="border-0 shadow-sm mb-4">
                    <Card.Body className="p-4">
                        <Row className="g-3">
                            <Col md={6}>
                                <Form.Group>
                                    <Form.Label className="fw-bold text-secondary">Site Seçimi</Form.Label>
                                    <Form.Select value={selectedSiteId} onChange={(e) => setSelectedSiteId(e.target.value)}>
                                        <option value="">Lütfen site seçiniz...</option>
                                        {sites.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                                    </Form.Select>
                                </Form.Group>
                            </Col>
                            <Col md={6}>
                                <Form.Group>
                                    <Form.Label className="fw-bold text-secondary">Genel Kurul Dönemi</Form.Label>
                                    <Form.Select 
                                        value={selectedAssemblyId} 
                                        onChange={(e) => setSelectedAssemblyId(e.target.value)}
                                        disabled={!selectedSiteId}
                                    >
                                        <option value="">Lütfen genel kurul seçiniz...</option>
                                        {assemblies.map(a => <option key={a.id} value={a.id}>{a.term} - {new Date(a.meetingDate).toLocaleDateString('tr-TR')}</option>)}
                                    </Form.Select>
                                </Form.Group>
                            </Col>
                        </Row>
                    </Card.Body>
                </Card>

                {assemblyDetail && (
                    <Card className="border-0 shadow-sm">
                        <CardHeader className="bg-white border-bottom-0 pt-4 px-4 d-flex align-items-center justify-content-between">
                            <h4 className="mb-0 fw-bold d-flex align-items-center gap-2">
                                <LuBookOpen className="text-info" /> Alınan Kararlar
                            </h4>
                            <div className="d-flex gap-2">
                                <Button variant="outline-info" size="sm" onClick={addDecision} className="d-flex align-items-center gap-1">
                                    <LuPlus /> Karar Ekle
                                </Button>
                                <Button variant="info" size="sm" className="text-white" onClick={save}>Kararları Kaydet</Button>
                            </div>
                        </CardHeader>
                        <Card.Body className="p-0 mt-2">
                            <div className="table-responsive">
                                <Table hover className="table-centered mb-0">
                                    <thead className="bg-light">
                                        <tr>
                                            <th className="ps-4" style={{ width: '10%' }}>No</th>
                                            <th style={{ width: '80%' }}>Karar Açıklaması</th>
                                            <th className="text-end pe-4">İşlem</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {(!assemblyDetail.decisions || assemblyDetail.decisions.length === 0) ? (
                                            <tr><td colSpan={3} className="text-center py-5 text-muted">Henüz karar girişi yapılmamış.</td></tr>
                                        ) : assemblyDetail.decisions.map((decision: any, idx: number) => (
                                            <tr key={idx}>
                                                <td className="ps-4 fw-bold">{decision.decisionNumber}</td>
                                                <td>
                                                    <Form.Control 
                                                        as="textarea"
                                                        rows={2}
                                                        size="sm"
                                                        placeholder="Karar metnini giriniz..."
                                                        value={decision.description}
                                                        onChange={(e) => updateDecision(idx, 'description', e.target.value)}
                                                    />
                                                </td>
                                                <td className="text-end pe-4">
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
                    </Card>
                )}
            </Container>
        </VerticalLayout>
    )
}

export default DecisionsPage
