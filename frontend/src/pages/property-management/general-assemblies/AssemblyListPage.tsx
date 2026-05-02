import { useCallback, useEffect, useState, memo } from 'react'
import { Button, Card, Col, Container, Form, Modal, Row, Table, Badge, CardHeader, CardFooter, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPlus, LuSearch, LuPencil, LuTrash2, LuFileText, LuPrinter, LuMail, LuSettings2, LuFilter, LuCalendar, LuMapPin, LuInfo } from 'react-icons/lu'
import { TbGavel } from 'react-icons/tb'
import { showToast } from '@/utils/toast'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import TablePagination from '@/components/table/TablePagination'
import ConfirmationModal from '@/components/ConfirmationModal'
import PropertyManagementService from '@/services/propertyManagementService'
import type { SiteListItem, GeneralAssemblyListItem, GeneralAssemblyPayload, GeneralAssemblyDetail } from '@/types/propertyManagement'
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
    const [selectedSiteId, setSelectedSiteId] = useState('')

    // Generate years for Term selection (Last year, Current year, Next year)
    const currentYear = new Date().getFullYear()
    const years = [(currentYear + 1).toString(), currentYear.toString(), (currentYear - 1).toString()]

    // Sorting assemblies by term descending
    const sortedAssemblies = [...assemblies].sort((a, b) => b.term.localeCompare(a.term))

    // Confirmation Modal States
    const [deleteConfirmShow, setDeleteConfirmShow] = useState(false)
    const [idToDelete, setIdToDelete] = useState<string | null>(null)
    const [isDeleting, setIsDeleting] = useState(false)

    const initialForm: GeneralAssemblyPayload = {
        siteId: '',
        meetingDate: new Date().toISOString().slice(0, 16),
        secondMeetingDate: '',
        term: '',
        location: '',
        type: MeetingType.Ordinary,
        isCompleted: false,
        agendaItems: [{ order: 1, description: 'Açılış ve yoklama' }],
        decisions: [],
        boardMembers: []
    }
    const [form, setForm] = useState<GeneralAssemblyPayload>(initialForm)
    const [hazirunShow, setHazirunShow] = useState(false)
    const [selectedHazirunSite, setSelectedHazirunSite] = useState<string | null>(null)
    const [selectedHazirunDate, setSelectedHazirunDate] = useState<string>('')
    const [selectedHazirunLocation, setSelectedHazirunLocation] = useState<string>('')
    const [hazirunResidents, setHazirunResidents] = useState<any[]>([])
    const [hazirunLoading, setHazirunLoading] = useState(false)
    const [selectedHazirunAssembly, setSelectedHazirunAssembly] = useState<GeneralAssemblyListItem | null>(null)
    
    const [invitationShow, setInvitationShow] = useState(false)
    const [selectedAssembly, setSelectedAssembly] = useState<GeneralAssemblyListItem | null>(null)
    const [assemblyDetail, setAssemblyDetail] = useState<GeneralAssemblyDetail | null>(null)
    const [invitationLoading, setInvitationLoading] = useState(false)

    const fetchAssemblies = useCallback(async () => {
        setLoading(true)
        try {
            const result = await PropertyManagementService.getGeneralAssemblies({ 
                pageNumber, 
                pageSize, 
                searchTerm,
                siteId: selectedSiteId || undefined
            })
            setAssemblies(result.items)
            setTotalCount(result.totalCount)
        } catch (error) {
            showToast(t('common.error'), 'danger')
        } finally {
            setLoading(false)
        }
    }, [pageNumber, pageSize, searchTerm, selectedSiteId, t])

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

    const confirmRemove = (id: string) => {
        setIdToDelete(id)
        setDeleteConfirmShow(true)
    }

    const handleRemove = async () => {
        if (!idToDelete) return
        setIsDeleting(true)
        try {
            await PropertyManagementService.archiveGeneralAssembly(idToDelete)
            showToast(t('common.success'), 'success')
            fetchAssemblies()
            setDeleteConfirmShow(false)
        } catch (error) {
            showToast(t('common.error'), 'danger')
        } finally {
            setIsDeleting(false)
            setIdToDelete(null)
        }
    }

    const formatDate = (dateStr: string) => {
        if (!dateStr) return '-'
        return new Date(dateStr).toLocaleString('tr-TR', {
            weekday: 'long',
            day: 'numeric',
            month: 'long',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        })
    }

    const openHazirun = async (assembly: GeneralAssemblyListItem) => {
        setSelectedHazirunAssembly(assembly)
        setSelectedHazirunSite(assembly.siteId)
        setSelectedHazirunDate(assembly.meetingDate)
        setSelectedHazirunLocation(assembly.location || '')
        setHazirunShow(true)
        setHazirunLoading(true)
        try {
            const [unitsResult, residentsResult] = await Promise.all([
                PropertyManagementService.getUnits({ siteId: assembly.siteId, pageNumber: 1, pageSize: 100 }),
                PropertyManagementService.getResidents({ siteId: assembly.siteId, type: 1, pageNumber: 1, pageSize: 100 })
            ])

            const merged = unitsResult.items.map(unit => {
                const unitOwners = residentsResult.items.filter(r => r.unitId === unit.id)
                return {
                    blockName: unit.blockName,
                    unitNumber: unit.number,
                    landShare: unit.landShare,
                    owners: unitOwners.length > 0 ? unitOwners : [{ firstName: '', lastName: '' }]
                }
            })

            const displayData = merged.flatMap(item => 
                item.owners.map(owner => ({
                    blockName: item.blockName,
                    unitNumber: item.unitNumber,
                    landShare: item.landShare,
                    firstName: owner.firstName,
                    lastName: owner.lastName
                }))
            )

            setHazirunResidents(displayData)
        } catch (error) {
            showToast(t('common.error'), 'danger')
        } finally {
            setHazirunLoading(false)
        }
    }

    const openInvitation = async (assembly: GeneralAssemblyListItem) => {
        setAssemblyDetail(null)
        setSelectedAssembly(assembly)
        setInvitationShow(true)
        setInvitationLoading(true)
        try {
            const data = await PropertyManagementService.getGeneralAssembly(assembly.id)
            setAssemblyDetail(data)
        } catch (error) {
            showToast(t('common.error'), 'danger')
        } finally {
            setInvitationLoading(false)
        }
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title="Genel Kurul İşlemleri" subtitle="Genel Kurullar & Hazirun Cetveli" />
            <Container fluid>
                <Card className="border-0 shadow-sm overflow-hidden">
                    <CardHeader className="border-bottom border-light p-3">
                        <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 w-100">
                            <div className="d-flex flex-wrap gap-2 align-items-center">
                                {/* Search */}
                                <div className="app-search" style={{ minWidth: '240px' }}>
                                    <input
                                        value={searchTerm}
                                        onChange={(e) => {
                                            setSearchTerm(e.target.value)
                                            setPageNumber(1)
                                        }}
                                        type="search"
                                        className="form-control"
                                        placeholder={t('common.search') + "..."}
                                    />
                                    <LuSearch className="app-search-icon text-muted" />
                                </div>

                                {/* Site Filter */}
                                <div className="app-search" style={{ minWidth: '200px' }}>
                                    <Form.Select
                                        value={selectedSiteId}
                                        onChange={(e) => {
                                            setSelectedSiteId(e.target.value)
                                            setPageNumber(1)
                                        }}
                                    >
                                        <option value="">Tüm Siteler</option>
                                        {sites.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                                    </Form.Select>
                                    <LuFilter className="app-search-icon text-muted" />
                                </div>

                                {/* Page Size */}
                                <div className="app-search" style={{ minWidth: '140px' }}>
                                    <Form.Select
                                        value={pageSize}
                                        onChange={(e) => {
                                            setPageSize(Number(e.target.value))
                                            setPageNumber(1)
                                        }}
                                        className="ps-4"
                                    >
                                        {[10, 20, 50, 100].map((size) => (
                                            <option key={size} value={size}>
                                                {size} {t('identity.table.show')}
                                            </option>
                                        ))}
                                    </Form.Select>
                                    <LuSettings2 className="app-search-icon text-muted" />
                                </div>
                                <Button variant="outline-primary" onClick={fetchAssemblies}>{t('common.search')}</Button>
                            </div>

                            <Button variant="primary" className="px-3 shadow-sm" onClick={openCreate}>
                                <LuPlus className="me-1" /> Yeni Genel Kurul
                            </Button>
                        </div>
                    </CardHeader>
                    <Card.Body className="p-0 mt-2">
                        <div className="table-responsive">
                            <Table hover className="table-custom table-centered mb-0">
                                <thead className="bg-light align-middle bg-opacity-25 thead-sm">
                                    <tr className="text-uppercase fs-xxs">
                                        <th className="ps-4" style={{ width: '80px' }}>#</th>
                                        <th>{t('property.generalAssembly.siteName')}</th>
                                        <th>{t('property.generalAssembly.term')}</th>
                                        <th>{t('property.generalAssembly.meetingDate')}</th>
                                        <th>{t('property.generalAssembly.type')}</th>
                                        <th className="text-end pe-4">{t('common.actions')}</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {loading ? (
                                        <tr><td colSpan={7} className="text-center py-4">Yükleniyor...</td></tr>
                                    ) : sortedAssemblies.length === 0 ? (
                                        <tr><td colSpan={7} className="text-center py-4 text-muted">Kayıt bulunamadı.</td></tr>
                                    ) : sortedAssemblies.map((a) => (
                                        <tr key={a.id}>
                                            <td className="ps-4">
                                                <div className="avatar-sm bg-light rounded d-flex align-items-center justify-content-center">
                                                    <TbGavel className="text-primary fs-20" />
                                                </div>
                                            </td>
                                            <td className="fw-medium">{a.siteName}</td>
                                            <td>
                                                <Badge bg="soft-info" className="text-info fs-12 px-2 py-1">
                                                    {a.term}
                                                </Badge>
                                            </td>
                                            <td>
                                                <div className="d-flex flex-column">
                                                    <div className="d-flex align-items-center">
                                                        <LuCalendar className="me-1 text-muted fs-12" />
                                                        {formatDate(a.meetingDate)}
                                                    </div>
                                                    {a.secondMeetingDate && (
                                                        <small className="text-muted fs-11">
                                                            <LuInfo className="me-1" />
                                                            {t('property.generalAssembly.secondDate')}: {formatDate(a.secondMeetingDate)}
                                                        </small>
                                                    )}
                                                </div>
                                            </td>
                                            <td>
                                                <Badge bg={a.type === MeetingType.Ordinary ? 'soft-info' : 'soft-danger'} className={`px-2 py-1 ${a.type === MeetingType.Ordinary ? 'text-info' : 'text-danger'}`}>
                                                    {a.type === MeetingType.Ordinary ? t('property.generalAssembly.typeOrdinary') : t('property.generalAssembly.typeExtraordinary')}
                                                </Badge>
                                            </td>
                                            <td className="text-end pe-4">
                                                <div className="d-flex justify-content-end gap-1">
                                                    <OverlayTrigger placement="top" overlay={<Tooltip>{t('property.generalAssembly.hazirunList')}</Tooltip>}>
                                                        <Button 
                                                            variant="soft-info" 
                                                            size="sm" 
                                                            className="btn-icon"
                                                            onClick={() => openHazirun(a)}
                                                        >
                                                            <LuFileText size={16} />
                                                        </Button>
                                                    </OverlayTrigger>

                                                    <OverlayTrigger placement="top" overlay={<Tooltip>{t('property.generalAssembly.invitationLetter')}</Tooltip>}>
                                                        <Button 
                                                            variant="soft-secondary" 
                                                            size="sm" 
                                                            className="btn-icon"
                                                            onClick={() => openInvitation(a)}
                                                        >
                                                            <LuMail size={16} />
                                                        </Button>
                                                    </OverlayTrigger>

                                                    <OverlayTrigger placement="top" overlay={<Tooltip>{t('common.edit')}</Tooltip>}>
                                                        <Button 
                                                            variant="soft-primary" 
                                                            size="sm" 
                                                            className="btn-icon"
                                                            onClick={() => openEdit(a.id)}
                                                        >
                                                            <LuPencil size={16} />
                                                        </Button>
                                                    </OverlayTrigger>

                                                    <OverlayTrigger placement="top" overlay={<Tooltip>{t('common.delete')}</Tooltip>}>
                                                        <Button 
                                                            variant="soft-danger" 
                                                            size="sm" 
                                                            className="btn-icon"
                                                            onClick={() => confirmRemove(a.id)}
                                                        >
                                                            <LuTrash2 size={16} />
                                                        </Button>
                                                    </OverlayTrigger>
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
                assembly={selectedHazirunAssembly}
                currentDate={selectedHazirunDate}
                setCurrentDate={setSelectedHazirunDate}
            />

            <InvitationModal 
                show={invitationShow}
                onHide={() => setInvitationShow(false)}
                assembly={selectedAssembly}
                detail={assemblyDetail}
                loading={invitationLoading}
            />

            <ConfirmationModal
                show={deleteConfirmShow}
                onHide={() => setDeleteConfirmShow(false)}
                onConfirm={handleRemove}
                title={t('common.delete')}
                message={t('common.areYouSure')}
                variant="danger"
                isLoading={isDeleting}
                confirmText={t('common.delete')}
            />

            <style>{`
                .app-search { position: relative; }
                .app-search .form-control, .app-search .form-select { padding-left: 2.5rem !important; border-radius: 4px; }
                .app-search-icon { position: absolute; left: 12px; top: 50%; transform: translateY(-50%); font-size: 16px; z-index: 1; }
                .btn-icon { width: 32px; height: 32px; padding: 0; display: flex; align-items: center; justify-content: center; transition: all 0.2s; background: transparent; border: 1px solid transparent; }
                .btn-icon:hover { background-color: var(--bs-tertiary-bg) !important; transform: translateY(-2px); box-shadow: var(--bs-box-shadow-sm) !important; border-color: var(--bs-border-color); }
                .table-custom tr:hover { background-color: rgba(var(--bs-primary-rgb), 0.05); }
                .thead-sm th { padding-top: 0.5rem !important; padding-bottom: 0.5rem !important; font-weight: 600; color: var(--bs-secondary-color); }
                .fs-xxs { font-size: 0.7rem !important; letter-spacing: 0.02em; }
                
                [data-bs-theme="dark"] .app-search .form-control,
                [data-bs-theme="dark"] .app-search .form-select {
                    background-color: var(--bs-tertiary-bg);
                    border-color: var(--bs-border-color);
                }
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
                                <Form.Label>Dönem (Yıl)</Form.Label>
                                <Form.Select 
                                    value={form.term} 
                                    onChange={(e) => setForm({ ...form, term: e.target.value })} 
                                >
                                    <option value="">Yıl Seçiniz...</option>
                                    {[new Date().getFullYear() + 1, new Date().getFullYear(), new Date().getFullYear() - 1].map(year => (
                                        <option key={year} value={year.toString()}>{year}</option>
                                    ))}
                                </Form.Select>
                            </Form.Group>
                        </Col>
                        <Col md={12}>
                            <Form.Group>
                                <Form.Label>Toplantı Yeri (Adres/Salon)</Form.Label>
                                <Form.Control 
                                    type="text" 
                                    placeholder="Örn: Site Sosyal Tesisi veya Site Bahçesi" 
                                    value={form.location || ''} 
                                    onChange={(e) => setForm({ ...form, location: e.target.value })} 
                                />
                            </Form.Group>
                        </Col>
                        <Col md={6}>
                            <Form.Group>
                                <Form.Label>Toplantı Tarihi ve Saati</Form.Label>
                                <Form.Control 
                                    type="datetime-local" 
                                    value={form.meetingDate?.slice(0, 16)} 
                                    onChange={(e) => {
                                        const dateVal = e.target.value;
                                        if (!dateVal) return;
                                        
                                        const d = new Date(dateVal);
                                        d.setDate(d.getDate() + 7);
                                        
                                        const pad = (n: number) => n.toString().padStart(2, '0');
                                        const secondDate = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
                                        
                                        setForm({ 
                                            ...form, 
                                            meetingDate: dateVal,
                                            secondMeetingDate: secondDate
                                        });
                                    }} 
                                />
                            </Form.Group>
                        </Col>
                        <Col md={6}>
                            <Form.Group>
                                <Form.Label>2. Toplantı Tarihi (Çoğunluk Sağlanmazsa)</Form.Label>
                                <Form.Control 
                                    type="datetime-local" 
                                    value={form.secondMeetingDate?.slice(0, 16)} 
                                    onChange={(e) => setForm({ ...form, secondMeetingDate: e.target.value })} 
                                />
                                <Form.Text className="text-muted fs-xs">Genelde ilk toplantıdan 7-15 gün sonradır.</Form.Text>
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

const HazirunModal = memo(({ show, onHide, residents, loading, assembly, currentDate, setCurrentDate }: any) => {
    const handlePrint = () => {
        window.print()
    }

    const formatDate = (dateStr: string) => {
        if (!dateStr) return '-'
        return new Date(dateStr).toLocaleString('tr-TR', {
            weekday: 'long',
            day: 'numeric',
            month: 'long',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        })
    }

    return (
        <Modal show={show} onHide={onHide} size="lg" centered scrollable>
            <Modal.Header closeButton className="print-hide">
                <div className="d-flex align-items-center justify-content-between w-100 me-2">
                    <Modal.Title>Hazirun Cetveli</Modal.Title>
                    {assembly?.secondMeetingDate && (
                        <div className="d-flex gap-1 ms-auto">
                            <Button 
                                variant={currentDate === assembly.meetingDate ? 'primary' : 'outline-primary'} 
                                size="sm"
                                onClick={() => setCurrentDate(assembly.meetingDate)}
                                className="px-3"
                            >
                                1. Toplantı
                            </Button>
                            <Button 
                                variant={currentDate === assembly.secondMeetingDate ? 'primary' : 'outline-primary'} 
                                size="sm"
                                onClick={() => setCurrentDate(assembly.secondMeetingDate)}
                                className="px-3"
                            >
                                2. Toplantı
                            </Button>
                        </div>
                    )}
                </div>
            </Modal.Header>
            <Modal.Body className="p-0">
                <div className="p-4 hazirun-print-area">
                    <div className="text-center mb-4 border-bottom pb-3">
                        <h3 className="fw-bold mb-1 text-uppercase">{assembly?.siteName} SİTESİ</h3>
                        <h4 className="fw-bold mb-1">HAZİRUN CETVELİ</h4>
                        <p className="text-muted mb-0">
                            {currentDate === assembly?.secondMeetingDate ? '2. Toplantı' : '1. Toplantı'} Katılım Listesi
                        </p>
                        <div className="d-flex justify-content-center gap-4 mt-3 fs-sm fw-bold">
                            <div>Tarih: {formatDate(currentDate)}</div>
                            {assembly?.location && <div>Yer: {assembly.location}</div>}
                        </div>
                    </div>
                    <Table bordered className="hazirun-table">
                        <thead className="bg-light">
                            <tr>
                                <th style={{ width: '10%' }}>Blok/No</th>
                                <th style={{ width: '45%' }}>Kat Maliki Adı Soyadı</th>
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
                                    <td></td>
                                    <td style={{ height: '40px' }}></td>
                                </tr>
                            ))}
                        </tbody>
                    </Table>
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
                    @page { 
                        size: auto;
                        margin: 0 !important; 
                    }
                    body { 
                        margin: 0 !important; 
                        padding: 0 !important;
                        background-color: white !important;
                    }
                    .print-hide { display: none !important; }
                    .modal { position: absolute !important; left: 0 !important; top: 0 !important; margin: 0 !important; padding: 0 !important; min-height: 100% !important; width: 100% !important; }
                    .modal-dialog { max-width: 100% !important; margin: 0 !important; padding: 0 !important; }
                    .modal-content { border: none !important; box-shadow: none !important; }
                    .hazirun-print-area { 
                        padding: 15mm !important; 
                        color: black !important;
                    }
                }
                .hazirun-table th, .hazirun-table td { 
                    padding: 4px 8px !important; 
                    vertical-align: middle; 
                    border: 1px solid #dee2e6 !important;
                    font-size: 0.85rem;
                }
                .hazirun-print-area h3 { font-size: 1.25rem; }
                .hazirun-print-area h4 { font-size: 1.1rem; }
            `}</style>
        </Modal>
    )
})

const InvitationModal = memo(({ show, onHide, assembly, detail, loading }: any) => {
    const handlePrint = () => {
        window.print()
    }

    const formatDate = (dateStr: string) => {
        if (!dateStr) return '-'
        return new Date(dateStr).toLocaleString('tr-TR', {
            weekday: 'long',
            day: 'numeric',
            month: 'long',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        })
    }

    return (
        <Modal show={show} onHide={onHide} size="lg" centered scrollable>
            <Modal.Header closeButton className="print-hide">
                <Modal.Title>Toplantı Davet Dilekçesi</Modal.Title>
            </Modal.Header>
            <Modal.Body className="p-0">
                {loading ? (
                    <div className="p-5 text-center">Yükleniyor...</div>
                ) : assembly && (
                    <div className="invitation-print-area fs-5">
                        <div className="text-center mb-5">
                            <h2 className="fw-bold mb-1">{assembly.siteName.toUpperCase()}</h2>
                            <h4 className="fw-bold text-decoration-underline">SİTE YÖNETİM KURULU BAŞKANLIĞI'NDAN</h4>
                        </div>

                        <div className="mb-4">
                            <p className="fw-bold">Sayın Kat Maliki,</p>
                            <p style={{ textAlign: 'justify', lineHeight: '1.6' }}>
                                Sitemizin <strong>{assembly.term}</strong> yılı Olağan Kat Malikleri Kurulu toplantısı, 
                                aşağıda belirtilen gündem maddelerini görüşmek ve karara bağlamak üzere 
                                <strong> {formatDate(assembly.meetingDate)}</strong> tarihinde, 
                                <strong> {assembly.location || 'Site Toplantı Salonu'}</strong> adresinde yapılacaktır.
                            </p>
                            
                            {assembly.secondMeetingDate && (
                                <p style={{ textAlign: 'justify', lineHeight: '1.6' }}>
                                    İlk toplantıda yasal çoğunluk sağlanamadığı takdirde, ikinci toplantı çoğunluk aranmaksızın 
                                    <strong> {formatDate(assembly.secondMeetingDate)}</strong> tarihinde aynı yer ve saatte yapılacaktır.
                                </p>
                            )}

                            <p style={{ textAlign: 'justify' }}>
                                Tüm kat maliklerinin toplantıya bizzat katılmaları veya kendilerini bir vekil aracılığı ile temsil ettirmeleri önemle rica olunur.
                            </p>
                        </div>

                        <div className="mb-4">
                            <h5 className="fw-bold text-decoration-underline mb-3">TOPLANTI GÜNDEMİ:</h5>
                            <ol>
                                {detail?.agendaItems?.map((item: any, idx: number) => (
                                    <li key={idx} className="mb-2">{item.description}</li>
                                ))}
                                {(!detail?.agendaItems || detail.agendaItems.length === 0) && (
                                    <li>Gündem maddesi belirtilmemiştir.</li>
                                )}
                            </ol>
                        </div>

                        <div className="mt-5 pt-5 d-flex justify-content-end">
                            <div className="text-center" style={{ minWidth: '200px' }}>
                                <p className="mb-1">{new Date().toLocaleDateString('tr-TR')}</p>
                                <p className="fw-bold mb-0">{assembly.siteName}</p>
                                <p className="fw-bold">Yönetim Kurulu</p>
                            </div>
                        </div>
                        
                        {/* Page Break for Power of Attorney */}
                        <div className="page-break"></div>

                        <div className="invitation-print-area">
                            {[1, 2].map((i) => (
                                <div key={i} className={`vekaletname-box ${i === 1 ? 'border-bottom border-dashed' : ''}`} 
                                     style={{ height: '148mm', padding: '10mm 0', display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
                                    
                                    <div className="text-center mb-4">
                                        <h4 className="fw-bold text-decoration-underline">VEKALETNAME</h4>
                                    </div>

                                    <div className="mb-4">
                                        <h5 className="fw-bold mb-3">{assembly.siteName.toUpperCase()} SİTE KAT MALİKLERİ KURULU BAŞKANLIĞI'NA</h5>
                                        
                                        <p style={{ textAlign: 'justify', lineHeight: '1.6', fontSize: '1rem' }}>
                                            Sitemizin <strong>{formatDate(assembly.meetingDate)}</strong> tarihinde, 
                                            bu toplantıda yasal çoğunluk sağlanamadığı takdirde 
                                            <strong> {assembly.secondMeetingDate ? formatDate(assembly.secondMeetingDate) : '.../.../20...'}</strong> tarihinde 
                                            yapılacak olan Olağan Kat Malikleri Kurulu Toplantısında, beni / bizi temsile, gündemdeki maddelerin görüşülüp karara bağlanması için oy kullanmaya 
                                            vekil tayin ettim / ettik.
                                        </p>
                                    </div>

                                    <div className="row g-0">
                                        <div className="col-12">
                                            <Table bordered size="sm" className="mb-0" style={{ fontSize: '1rem' }}>
                                                <tbody>
                                                    <tr>
                                                        <td className="fw-bold" style={{ width: '40%' }}>VEKİL ADI SOYADI</td>
                                                        <td style={{ height: '35px' }}></td>
                                                    </tr>
                                                    <tr>
                                                        <td className="fw-bold">KAT MALİKİ ADI SOYADI</td>
                                                        <td style={{ height: '35px' }}></td>
                                                    </tr>
                                                    <tr>
                                                        <td className="fw-bold">BLOK / DAİRE NO</td>
                                                        <td style={{ height: '35px' }}></td>
                                                    </tr>
                                                    <tr>
                                                        <td className="fw-bold">İMZA</td>
                                                        <td style={{ height: '60px' }}></td>
                                                    </tr>
                                                </tbody>
                                            </Table>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </Modal.Body>
            <Modal.Footer className="print-hide">
                <Button variant="light" onClick={onHide}>Kapat</Button>
                <Button variant="primary" onClick={handlePrint} className="d-flex align-items-center gap-2">
                    <LuPrinter /> Yazdır / PDF
                </Button>
            </Modal.Footer>
            <style>{`
                @media print {
                    @page { 
                        size: auto;
                        margin: 0 !important; 
                    }
                    /* Hide everything in the app */
                    #root, .navbar, .sidebar, .modal-backdrop, .modal-header, .modal-footer { 
                        display: none !important; 
                    }
                    
                    /* Reset modal to be a normal block element for printing */
                    .modal { 
                        position: static !important; 
                        display: block !important; 
                        width: 100% !important;
                        height: auto !important;
                        overflow: visible !important;
                    }
                    .modal-dialog { 
                        max-width: 100% !important; 
                        margin: 0 !important; 
                        padding: 0 !important; 
                    }
                    .modal-content { 
                        border: none !important; 
                        box-shadow: none !important; 
                    }
                    .modal-body { 
                        padding: 0 !important;
                        overflow: visible !important;
                    }
                    
                    .invitation-print-area { 
                        display: block !important;
                        padding: 15mm !important; 
                        color: black !important;
                        background-color: white !important;
                    }
                    .page-break { 
                        clear: both; 
                        page-break-after: always; 
                        break-after: page; 
                    }
                    .invitation-print-area fs-5 { font-size: 1.1rem !important; }
                    body { background-color: white !important; }
                }
                .invitation-print-area { padding: 3rem; background-color: white; }
                .print-only { display: none; }
            `}</style>
        </Modal>
    )
})

export default AssemblyListPage
