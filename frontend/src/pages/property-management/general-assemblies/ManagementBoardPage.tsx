import { useCallback, useEffect, useState, memo } from 'react'
import { Button, Card, Col, Container, Form, Row, Table, Badge, CardHeader } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPlus, LuTrash2, LuUserPlus, LuShield } from 'react-icons/lu'
import { showToast } from '@/utils/toast'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import PropertyManagementService from '@/services/propertyManagementService'
import type { SiteListItem, GeneralAssemblyListItem, ResidentListItem, BoardMember } from '@/types/propertyManagement'
import { BoardType, BoardMemberType } from '@/types/propertyManagement'

const ManagementBoardPage = () => {
    const { t } = useTranslation()
    const [sites, setSites] = useState<SiteListItem[]>([])
    const [assemblies, setAssemblies] = useState<GeneralAssemblyListItem[]>([])
    const [residents, setResidents] = useState<ResidentListItem[]>([])
    
    const [selectedSiteId, setSelectedSiteId] = useState('')
    const [selectedAssemblyId, setSelectedAssemblyId] = useState('')
    const [assemblyDetail, setAssemblyDetail] = useState<any>(null)
    const [loading, setLoading] = useState(false)

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
            const fetchResidents = async () => {
                const result = await PropertyManagementService.getResidents({ siteId: selectedSiteId, pageNumber: 1, pageSize: 100 })
                setResidents(result.items)
            }
            fetchAssemblies()
            fetchResidents()
        } else {
            setAssemblies([])
            setResidents([])
        }
    }, [selectedSiteId])

    useEffect(() => {
        if (selectedAssemblyId) {
            const fetchDetail = async () => {
                setLoading(true)
                try {
                    const data = await PropertyManagementService.getGeneralAssembly(selectedAssemblyId)
                    setAssemblyDetail(data)
                } catch (error) {
                    showToast(t('common.error'), 'danger')
                } finally {
                    setLoading(false)
                }
            }
            fetchDetail()
        } else {
            setAssemblyDetail(null)
        }
    }, [selectedAssemblyId, t])

    const addMember = () => {
        if (!assemblyDetail) return
        const newMember: BoardMember = {
            residentId: '',
            boardType: BoardType.Management,
            memberType: BoardMemberType.Principal,
            title: ''
        }
        setAssemblyDetail({ ...assemblyDetail, boardMembers: [...assemblyDetail.boardMembers, newMember] })
    }

    const updateMember = (idx: number, field: keyof BoardMember, value: any) => {
        const newMembers = [...assemblyDetail.boardMembers]
        newMembers[idx] = { ...newMembers[idx], [field]: value }
        setAssemblyDetail({ ...assemblyDetail, boardMembers: newMembers })
    }

    const removeMember = (idx: number) => {
        const newMembers = assemblyDetail.boardMembers.filter((_: any, i: number) => i !== idx)
        setAssemblyDetail({ ...assemblyDetail, boardMembers: newMembers })
    }

    const save = async () => {
        try {
            await PropertyManagementService.updateGeneralAssembly(selectedAssemblyId, assemblyDetail)
            showToast(t('common.success'), 'success')
        } catch (error) {
            showToast(t('common.error'), 'danger')
        }
    }

    const mBoardMembers = assemblyDetail?.boardMembers?.filter((m: any) => m.boardType === BoardType.Management) || []

    return (
        <VerticalLayout>
            <PageBreadcrumb title="Genel Kurul İşlemleri" subtitle="Yönetim Kurulu Belirleme" />
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
                                <LuShield className="text-success" /> Yönetim Kurulu Üyeleri
                            </h4>
                            <div className="d-flex gap-2">
                                <Button variant="outline-success" size="sm" onClick={addMember} className="d-flex align-items-center gap-1">
                                    <LuPlus /> Üye Ekle
                                </Button>
                                <Button variant="success" size="sm" onClick={save}>Değişiklikleri Kaydet</Button>
                            </div>
                        </CardHeader>
                        <Card.Body className="p-0 mt-2">
                            <div className="table-responsive">
                                <Table hover className="table-centered mb-0">
                                    <thead className="bg-light">
                                        <tr>
                                            <th className="ps-4" style={{ width: '35%' }}>Kişi</th>
                                            <th style={{ width: '25%' }}>Üyelik Tipi</th>
                                            <th style={{ width: '30%' }}>Görev / Ünvan</th>
                                            <th className="text-end pe-4">İşlem</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {assemblyDetail.boardMembers.filter((m: any) => m.boardType === BoardType.Management).length === 0 ? (
                                            <tr><td colSpan={4} className="text-center py-5 text-muted">Henüz yönetim kurulu üyesi eklenmemiş.</td></tr>
                                        ) : assemblyDetail.boardMembers.map((member: any, idx: number) => {
                                            if (member.boardType !== BoardType.Management) return null;
                                            return (
                                                <tr key={idx}>
                                                    <td className="ps-4">
                                                        <Form.Select 
                                                            size="sm"
                                                            value={member.residentId}
                                                            onChange={(e) => updateMember(idx, 'residentId', e.target.value)}
                                                        >
                                                            <option value="">Seçiniz...</option>
                                                            {residents.map(r => <option key={r.id} value={r.id}>{r.firstName} {r.lastName} {r.unitNumber ? `(D:${r.unitNumber})` : ''}</option>)}
                                                        </Form.Select>
                                                    </td>
                                                    <td>
                                                        <Form.Select 
                                                            size="sm"
                                                            value={member.memberType}
                                                            onChange={(e) => updateMember(idx, 'memberType', Number(e.target.value))}
                                                        >
                                                            <option value={BoardMemberType.Principal}>Asil Üye</option>
                                                            <option value={BoardMemberType.Substitute}>Yedek Üye</option>
                                                        </Form.Select>
                                                    </td>
                                                    <td>
                                                        <Form.Control 
                                                            size="sm"
                                                            type="text" 
                                                            placeholder="Örn: Başkan, Sayman..." 
                                                            value={member.title || ''} 
                                                            onChange={(e) => updateMember(idx, 'title', e.target.value)}
                                                        />
                                                    </td>
                                                    <td className="text-end pe-4">
                                                        <Button variant="link" className="text-danger p-0" onClick={() => removeMember(idx)}>
                                                            <LuTrash2 />
                                                        </Button>
                                                    </td>
                                                </tr>
                                            )
                                        })}
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

export default ManagementBoardPage
