import { useCallback, useEffect, useState, memo } from 'react'
import { Button, Card, Col, Container, Form, Modal, Row, Table, Badge, CardHeader, CardFooter } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPlus, LuSearch, LuPencil, LuTrash2, LuFileText } from 'react-icons/lu'
import { showToast } from '@/utils/toast'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import TablePagination from '@/components/table/TablePagination'
import PropertyManagementService from '@/services/propertyManagementService'
import type { SiteListItem, GeneralAssemblyListItem, GeneralAssemblyPayload } from '@/types/propertyManagement'
import { MeetingType } from '@/types/propertyManagement'

const AssemblyListPage = () => {
    const { t } = useTranslation()
    const [assemblies, setAssemblies] = useState<GeneralAssemblyListItem[]>([])
    const [sites, setSites] = useState<SiteListItem[]>([])
    const [loading, setLoading] = useState(true)
    const [showModal, setShowModal] = useState(false)
    const [editingId, setEditingId] = useState<string | null>(null)
    const [totalCount, setTotalCount] = useState(0)
    const [pageNumber, setPageNumber] = useState(1)
    const [pageSize, setPageSize] = useState(10)
    const [searchTerm, setSearchTerm] = useState('')

    const initialForm: GeneralAssemblyPayload = {
        siteId: '',
        meetingDate: new Date().toISOString().split('T')[0],
        term: '',
        type: MeetingType.Ordinary,
        isCompleted: false,
        agendaItems: [{ order: 1, description: 'Açılış ve yoklama' }],
        decisions: [],
        boardMembers: []
    }
    const [form, setForm] = useState<GeneralAssemblyPayload>(initialForm)
    const [hazirunShow, setHazirunShow] = useState(false)
    const [selectedHazirunSite, setSelectedHazirunSite] = useState<string | null>(null)
    const [hazirunResidents, setHazirunResidents] = useState<any[]>([])
    const [hazirunLoading, setHazirunLoading] = useState(false)

    const fetchAssemblies = useCallback(async () => {
        setLoading(true)
        try {
            const result = await PropertyManagementService.getGeneralAssemblies({ pageNumber, pageSize, searchTerm })
            setAssemblies(result.items)
            setTotalCount(result.totalCount)
        } catch (error) {
            showToast(t('common.error'), 'danger')
        } finally {
            setLoading(false)
        }
    }, [pageNumber, pageSize, searchTerm, t])

    const fetchSites = useCallback(async () => {
        try {
            const result = await PropertyManagementService.getSites({ pageNumber: 1, pageSize: 100 })
            setSites(result.items)
        } catch (error) {}
    }, [])

    useEffect(() => {
        fetchAssemblies()
    }, [fetchAssemblies])

    useEffect(() => {
        fetchSites()
    }, [fetchSites])

    const openCreate = () => {
        setEditingId(null)
        setForm(initialForm)
        setShowModal(true)
    }

    const openEdit = async (id: string) => {
        try {
            const data = await PropertyManagementService.getGeneralAssembly(id)
            if (data) {
                setEditingId(id)
                setForm(data)
                setShowModal(true)
            }
        } catch (error) {
            showToast(t('common.error'), 'danger')
        }
    }

    const save = async () => {
        try {
            if (editingId) {
                await PropertyManagementService.updateGeneralAssembly(editingId, form)
                showToast(t('common.success'), 'success')
            } else {
                await PropertyManagementService.createGeneralAssembly(form)
                showToast(t('common.success'), 'success')
            }
            setShowModal(false)
            fetchAssemblies()
        } catch (error) {
            showToast(t('common.error'), 'danger')
        }
    }

    const remove = async (id: string) => {
        if (!window.confirm(t('common.areYouSure'))) return
        try {
            await PropertyManagementService.archiveGeneralAssembly(id)
            showToast(t('common.success'), 'success')
            fetchAssemblies()
        } catch (error) {
            showToast(t('common.error'), 'danger')
        }
    }

    const formatDate = (dateStr: string) => new Date(dateStr).toLocaleDateString('tr-TR')

    const openHazirun = async (siteId: string) => {
        setSelectedHazirunSite(siteId)
        setHazirunShow(true)
        setHazirunLoading(true)
        try {
            const result = await PropertyManagementService.getResidents({ siteId, pageNumber: 1, pageSize: 100 })
            setHazirunResidents(result.items)
        } catch (error) {
            showToast(t('common.error'), 'danger')
        } finally {
            setHazirunLoading(false)
        }
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title="Genel Kurul İşlemleri" subtitle="Genel Kurullar & Hazirun Cetveli" />
            <Container fluid>
                <Card className="border-0 shadow-sm">
                    <CardHeader className="bg-white border-bottom-0 pt-4 px-4">
                        <div className="d-flex align-items-center justify-content-between">
                            <h4 className="mb-0 fw-bold">Genel Kurul Listesi</h4>
                            <div className="d-flex gap-2">
                                <div className="d-flex align-items-center gap-2 me-3">
                                    <span className="text-muted fs-sm">Göster:</span>
                                    <Form.Select 
                                        size="sm" 
                                        value={pageSize} 
                                        onChange={(e) => { setPageSize(Number(e.target.value)); setPageNumber(1); }}
                                        style={{ width: '70px' }}
                                    >
                                        {[10, 20, 50, 100].map(size => <option key={size} value={size}>{size}</option>)}
                                    </Form.Select>
                                </div>
                                <Button variant="primary" onClick={openCreate} className="d-flex align-items-center gap-2">
                                    <LuPlus /> Yeni Genel Kurul
                                </Button>
                            </div>
                        </div>
                        <div className="mt-3">
                            <Form.Group className="position-relative">
                                <LuSearch className="position-absolute top-50 start-0 translate-middle-y ms-3 text-muted" />
                                <Form.Control 
                                    type="text" 
                                    placeholder="Dönem veya notlarda ara..." 
                                    className="ps-5 bg-light border-0"
                                    value={searchTerm}
                                    onChange={(e) => { setSearchTerm(e.target.value); setPageNumber(1); }}
                                />
                            </Form.Group>
                        </div>
                    </CardHeader>
                    <Card.Body className="p-0 mt-2">
                        <div className="table-responsive">
                            <Table hover className="table-centered table-nowrap mb-0">
                                <thead className="bg-light bg-opacity-50">
                                    <tr>
                                        <th className="ps-4">Site</th>
                                        <th>Dönem</th>
                                        <th>Toplantı Tarihi</th>
                                        <th>Tip</th>
                                        <th>Durum</th>
                                        <th className="text-end pe-4">İşlemler</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {loading ? (
                                        <tr><td colSpan={6} className="text-center py-5">Yükleniyor...</td></tr>
                                    ) : assemblies.length === 0 ? (
                                        <tr><td colSpan={6} className="text-center py-5 text-muted">Kayıt bulunamadı.</td></tr>
                                    ) : assemblies.map((a) => (
                                        <tr key={a.id}>
                                            <td className="ps-4 fw-medium">{a.siteName}</td>
                                            <td>{a.term}</td>
                                            <td>{formatDate(a.meetingDate)}</td>
                                            <td>
                                                <Badge bg={a.type === MeetingType.Ordinary ? 'info-subtle text-info' : 'danger-subtle text-danger'}>
                                                    {a.type === MeetingType.Ordinary ? 'Olağan' : 'Olağanüstü'}
                                                </Badge>
                                            </td>
                                            <td>
                                                {a.isCompleted ? (
                                                    <Badge bg="success-subtle text-success">Tamamlandı</Badge>
                                                ) : (
                                                    <Badge bg="warning-subtle text-warning">Bekliyor</Badge>
                                                )}
                                            </td>
                                            <td className="text-end pe-4">
                                                <div className="d-flex justify-content-end gap-1">
                                                    <Button 
                                                        variant="light" 
                                                        size="sm" 
                                                        title="Hazirun Cetveli" 
                                                        className="btn-icon text-primary"
                                                        onClick={() => openHazirun(a.siteId)}
                                                    >
                                                        <LuFileText />
                                                    </Button>
                                                    <Button variant="light" size="sm" onClick={() => openEdit(a.id)} className="btn-icon">
                                                        <LuPencil />
                                                    </Button>
                                                    <Button variant="light" size="sm" onClick={() => remove(a.id)} className="btn-icon text-danger">
                                                        <LuTrash2 />
                                                    </Button>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </Table>
                        </div>
                    </Card.Body>
                    <CardFooter className="bg-white border-top-0 pb-4">
                        <TablePagination
                            totalItems={totalCount}
                            start={(pageNumber - 1) * pageSize + 1}
                            end={Math.min(pageNumber * pageSize, totalCount)}
                            itemsName="genel kurul"
                            showInfo={true}
                            previousPage={() => setPageNumber(p => Math.max(1, p - 1))}
                            canPreviousPage={pageNumber > 1}
                            pageCount={Math.ceil(totalCount / pageSize)}
                            pageIndex={pageNumber - 1}
                            setPageIndex={(idx) => setPageNumber(idx + 1)}
                            nextPage={() => setPageNumber(p => Math.min(Math.ceil(totalCount / pageSize), p + 1))}
                            canNextPage={pageNumber < Math.ceil(totalCount / pageSize)}
                        />
                    </CardFooter>
                </Card>
            </Container>

            <AssemblyModal 
                show={showModal} 
                onHide={() => setShowModal(false)} 
                form={form} 
                setForm={setForm} 
                sites={sites} 
                onSave={save} 
                isEdit={!!editingId} 
            />

            <HazirunModal 
                show={hazirunShow}
                onHide={() => setHazirunShow(false)}
                residents={hazirunResidents}
                loading={hazirunLoading}
            />

            <style>{`
                .btn-icon { width: 32px; height: 32px; padding: 0; display: flex; align-items: center; justify-content: center; }
            `}</style>
        </VerticalLayout>
    )
}

const AssemblyModal = memo(({ show, onHide, form, setForm, sites, onSave, isEdit }: any) => {
    return (
        <Modal show={show} onHide={onHide} size="lg" centered>
            <Modal.Header closeButton>
                <Modal.Title>{isEdit ? 'Genel Kurul Düzenle' : 'Yeni Genel Kurul Tanımla'}</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <Form>
                    <Row className="g-3">
                        <Col md={6}>
                            <Form.Group>
                                <Form.Label>Site Seçimi</Form.Label>
                                <Form.Select 
                                    value={form.siteId} 
                                    onChange={(e) => setForm({ ...form, siteId: e.target.value })}
                                    disabled={isEdit}
                                >
                                    <option value="">Seçiniz...</option>
                                    {sites.map((s: any) => (
                                        <option key={s.id} value={s.id}>{s.name}</option>
                                    ))}
                                </Form.Select>
                            </Form.Group>
                        </Col>
                        <Col md={6}>
                            <Form.Group>
                                <Form.Label>Dönem</Form.Label>
                                <Form.Control 
                                    type="text" 
                                    placeholder="Örn: 2024-2025" 
                                    value={form.term} 
                                    onChange={(e) => setForm({ ...form, term: e.target.value })} 
                                />
                            </Form.Group>
                        </Col>
                        <Col md={6}>
                            <Form.Group>
                                <Form.Label>Toplantı Tarihi</Form.Label>
                                <Form.Control 
                                    type="date" 
                                    value={form.meetingDate.split('T')[0]} 
                                    onChange={(e) => setForm({ ...form, meetingDate: e.target.value })} 
                                />
                            </Form.Group>
                        </Col>
                        <Col md={6}>
                            <Form.Group>
                                <Form.Label>Toplantı Tipi</Form.Label>
                                <Form.Select 
                                    value={form.type} 
                                    onChange={(e) => setForm({ ...form, type: Number(e.target.value) })}
                                >
                                    <option value={MeetingType.Ordinary}>Olağan</option>
                                    <option value={MeetingType.Extraordinary}>Olağanüstü</option>
                                </Form.Select>
                            </Form.Group>
                        </Col>
                        <Col md={12}>
                            <Form.Check 
                                type="switch" 
                                label="Toplantı Tamamlandı" 
                                checked={form.isCompleted} 
                                onChange={(e) => setForm({ ...form, isCompleted: e.target.checked })} 
                            />
                        </Col>
                        <Col md={12}>
                            <div className="d-flex align-items-center justify-content-between mb-2">
                                <Form.Label className="mb-0 fw-bold">Gündem Maddeleri</Form.Label>
                                <Button 
                                    variant="outline-primary" 
                                    size="sm" 
                                    onClick={() => {
                                        const nextOrder = (form.agendaItems?.length || 0) + 1;
                                        setForm({ ...form, agendaItems: [...(form.agendaItems || []), { order: nextOrder, description: '' }] });
                                    }}
                                >
                                    <LuPlus size={14} /> Madde Ekle
                                </Button>
                            </div>
                            <div className="border rounded p-2 bg-light bg-opacity-25">
                                {(form.agendaItems || []).map((item: any, idx: number) => (
                                    <div key={idx} className="d-flex gap-2 mb-2">
                                        <div className="pt-2 fw-bold text-muted" style={{ minWidth: '20px' }}>{item.order}.</div>
                                        <Form.Control 
                                            size="sm"
                                            placeholder="Gündem maddesi açıklaması..."
                                            value={item.description}
                                            onChange={(e) => {
                                                const newItems = [...form.agendaItems];
                                                newItems[idx].description = e.target.value;
                                                setForm({ ...form, agendaItems: newItems });
                                            }}
                                        />
                                        <Button 
                                            variant="outline-danger" 
                                            size="sm" 
                                            onClick={() => {
                                                const newItems = form.agendaItems.filter((_: any, i: number) => i !== idx)
                                                    .map((item: any, i: number) => ({ ...item, order: i + 1 }));
                                                setForm({ ...form, agendaItems: newItems });
                                            }}
                                        >
                                            <LuTrash2 size={14} />
                                        </Button>
                                    </div>
                                ))}
                                {(!form.agendaItems || form.agendaItems.length === 0) && (
                                    <div className="text-center py-2 text-muted fs-xs">Henüz gündem maddesi eklenmedi.</div>
                                )}
                            </div>
                        </Col>
                    </Row>
                </Form>
            </Modal.Body>
            <Modal.Footer>
                <Button variant="light" onClick={onHide}>İptal</Button>
                <Button variant="primary" onClick={onSave}>Kaydet</Button>
            </Modal.Footer>
        </Modal>
    )
})

const HazirunModal = memo(({ show, onHide, residents, loading }: any) => {
    const handlePrint = () => {
        window.print()
    }

    return (
        <Modal show={show} onHide={onHide} size="lg" centered scrollable>
            <Modal.Header closeButton className="print-hide">
                <Modal.Title>Hazirun Cetveli (Katılım Listesi)</Modal.Title>
            </Modal.Header>
            <Modal.Body className="p-0">
                <div className="p-4 hazirun-print-area">
                    <div className="text-center mb-4">
                        <h3 className="fw-bold mb-1">HAZİRUN CETVELİ</h3>
                        <p className="text-muted">Kat Malikleri Kurulu Toplantısı Katılım Listesi</p>
                    </div>
                    <Table bordered className="hazirun-table">
                        <thead className="bg-light">
                            <tr>
                                <th style={{ width: '10%' }}>Blok/No</th>
                                <th style={{ width: '30%' }}>Kat Maliki Adı Soyadı</th>
                                <th style={{ width: '15%' }}>Arsa Payı</th>
                                <th style={{ width: '15%' }}>Temsilci</th>
                                <th style={{ width: '30%' }}>İmza</th>
                            </tr>
                        </thead>
                        <tbody>
                            {loading ? (
                                <tr><td colSpan={5} className="text-center py-4">Yükleniyor...</td></tr>
                            ) : residents.length === 0 ? (
                                <tr><td colSpan={5} className="text-center py-4 text-muted">Sakin kaydı bulunamadı.</td></tr>
                            ) : residents.map((r: any, idx: number) => (
                                <tr key={idx}>
                                    <td>{r.blockName} / {r.unitNumber}</td>
                                    <td>{r.firstName} {r.lastName}</td>
                                    <td>-</td>
                                    <td></td>
                                    <td style={{ height: '40px' }}></td>
                                </tr>
                            ))}
                        </tbody>
                    </Table>
                    <div className="mt-4 fs-xs text-muted print-only text-center">
                        Bu belge dijital ortamda Aparesk tarafından oluşturulmuştur.
                    </div>
                </div>
            </Modal.Body>
            <Modal.Footer className="print-hide">
                <Button variant="light" onClick={onHide}>Kapat</Button>
                <Button variant="primary" onClick={handlePrint} className="d-flex align-items-center gap-2">
                    Yazdır / PDF
                </Button>
            </Modal.Footer>
            <style>{`
                @media print {
                    .print-hide { display: none !important; }
                    .modal-dialog { max-width: 100% !important; margin: 0 !important; }
                    .modal-content { border: none !important; }
                    .hazirun-print-area { padding: 0 !important; }
                }
                .hazirun-table th, .hazirun-table td { padding: 8px; vertical-align: middle; border: 1px solid #dee2e6 !important; }
            `}</style>
        </Modal>
    )
})

export default AssemblyListPage
