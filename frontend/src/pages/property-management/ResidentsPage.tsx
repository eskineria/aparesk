import { useCallback, useEffect, useState } from 'react'
import { Badge, Button, Card, CardFooter, CardHeader, Col, Container, Form, Modal, Row, Table } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPencil, LuPlus, LuSearch, LuTrash2 } from 'react-icons/lu'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import TablePagination from '@/components/table/TablePagination'
import PropertyManagementService from '@/services/propertyManagementService'
import type {
    ResidentDetail,
    ResidentListItem,
    ResidentPayload,
    SiteListItem,
    UnitListItem,
} from '@/types/propertyManagement'
import { ResidentType } from '@/types/propertyManagement'
import { showToast } from '@/utils/toast'
import type { TFunction } from 'i18next'

const pageSize = 10
const lookupPageSize = 100

const emptyResident: ResidentPayload = {
    siteId: '',
    unitId: null,
    firstName: '',
    lastName: '',
    identityNumber: '',
    type: ResidentType.Owner,
    phone: '',
    email: '',
    occupation: '',
    emergencyContactName: '',
    emergencyContactPhone: '',
    moveInDate: '',
    moveOutDate: '',
    kvkkConsentGiven: false,
    communicationConsentGiven: false,
    notes: '',
    isActive: true,
}

const statusBadge = (isActive: boolean, isArchived: boolean, t: TFunction) => {
    if (isArchived) return <Badge bg="secondary-subtle text-secondary">{t('propertyManagement.status.archived')}</Badge>
    return <Badge bg={isActive ? 'success-subtle text-success' : 'warning-subtle text-warning'}>{t(isActive ? 'propertyManagement.status.active' : 'propertyManagement.status.inactive')}</Badge>
}

const residentTypeLabel = (type: ResidentType, t: TFunction) => t(`propertyManagement.residentTypes.${type}`, {
    defaultValue: t('propertyManagement.residentTypes.99'),
})

const formatDate = (value?: string | null) => value ? value.slice(0, 10) : '-'
const dateOrNull = (value: string) => value.trim() === '' ? null : value

const ResidentsPage = () => {
    const { t } = useTranslation()
    const [residents, setResidents] = useState<ResidentListItem[]>([])
    const [sites, setSites] = useState<SiteListItem[]>([])
    const [units, setUnits] = useState<UnitListItem[]>([])
    const [residentPage, setResidentPage] = useState(1)
    const [residentTotal, setResidentTotal] = useState(0)
    const [residentSearch, setResidentSearch] = useState('')
    const [showResidentModal, setShowResidentModal] = useState(false)
    const [editingResidentId, setEditingResidentId] = useState<string | null>(null)
    const [residentForm, setResidentForm] = useState<ResidentPayload>(emptyResident)

    const fetchResidents = useCallback(async () => {
        const result = await PropertyManagementService.getResidents({ pageNumber: residentPage, pageSize, searchTerm: residentSearch || undefined })
        setResidents(result.items)
        setResidentTotal(result.totalCount)
    }, [residentPage, residentSearch])

    const fetchSites = useCallback(async () => {
        try {
            const result = await PropertyManagementService.getSites({ pageNumber: 1, pageSize: lookupPageSize })
            setSites(result.items.filter(site => !site.isArchived))
        } catch (error) {
            console.error(error)
            showToast(t('common.error'), 'error')
        }
    }, [t])

    const fetchUnits = useCallback(async () => {
        try {
            const result = await PropertyManagementService.getUnits({ pageNumber: 1, pageSize: lookupPageSize })
            setUnits(result.items.filter(unit => !unit.isArchived))
        } catch (error) {
            console.error(error)
            showToast(t('common.error'), 'error')
        }
    }, [t])

    useEffect(() => { void fetchResidents() }, [fetchResidents])
    useEffect(() => { void fetchSites() }, [fetchSites])
    useEffect(() => { void fetchUnits() }, [fetchUnits])

    const openNewResident = () => {
        if (sites.length === 0) {
            showToast(t('propertyManagement.messages.noSites'), 'warning')
            return
        }

        setEditingResidentId(null)
        setResidentForm({ ...emptyResident, siteId: sites[0].id })
        setShowResidentModal(true)
    }

    const openEditResident = async (id: string) => {
        const resident = await PropertyManagementService.getResident(id)
        if (!resident) return
        setEditingResidentId(id)
        setResidentForm(mapResidentToPayload(resident))
        setShowResidentModal(true)
    }

    const saveResident = async () => {
        const payload = normalizeResidentPayload(residentForm)

        if (editingResidentId) {
            await PropertyManagementService.updateResident(editingResidentId, {
                unitId: payload.unitId,
                firstName: payload.firstName,
                lastName: payload.lastName,
                identityNumber: payload.identityNumber,
                type: payload.type,
                phone: payload.phone,
                email: payload.email,
                occupation: payload.occupation,
                emergencyContactName: payload.emergencyContactName,
                emergencyContactPhone: payload.emergencyContactPhone,
                moveInDate: payload.moveInDate,
                moveOutDate: payload.moveOutDate,
                kvkkConsentGiven: payload.kvkkConsentGiven,
                communicationConsentGiven: payload.communicationConsentGiven,
                notes: payload.notes,
                isActive: payload.isActive,
            })
            showToast(t('propertyManagement.messages.residentUpdated'), 'success')
        } else {
            await PropertyManagementService.createResident(payload)
            showToast(t('propertyManagement.messages.residentCreated'), 'success')
        }

        setShowResidentModal(false)
        await fetchResidents()
    }

    const archiveResident = async (id: string) => {
        await PropertyManagementService.archiveResident(id)
        showToast(t('propertyManagement.messages.residentArchived'), 'success')
        await fetchResidents()
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('propertyManagement.title')} subtitle={t('propertyManagement.tabs.residents')} />
            <Container fluid>
                <Card className="border-0 shadow-sm overflow-hidden mb-3">
                    <CardHeader className="border-bottom border-light p-3">
                        <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 w-100">
                            <div className="d-flex flex-wrap gap-2 align-items-center">
                                <div className="app-search" style={{ minWidth: '280px' }}>
                                    <input
                                        value={residentSearch}
                                        onChange={(e) => setResidentSearch(e.target.value)}
                                        onKeyDown={(e) => e.key === 'Enter' && fetchResidents()}
                                        type="search"
                                        className="form-control"
                                        placeholder={t('common.search') + "..."}
                                    />
                                    <LuSearch className="app-search-icon text-muted" />
                                </div>
                                <Button variant="outline-primary" onClick={() => { setResidentPage(1); void fetchResidents() }}>{t('common.search')}</Button>
                            </div>
                            <Button variant="primary" className="px-3 shadow-sm" onClick={openNewResident}>
                                <LuPlus className="me-1" /> {t('propertyManagement.actions.addResident')}
                            </Button>
                        </div>
                    </CardHeader>

                    <div className="table-responsive">
                        <Table hover className="table-custom table-centered mb-0">
                            <thead className="bg-light align-middle bg-opacity-25 thead-sm">
                                <tr className="text-uppercase fs-xxs">
                                    <th>{t('propertyManagement.fields.site')}</th>
                                    <th>{t('propertyManagement.fields.unit')}</th>
                                    <th>{t('propertyManagement.fields.resident')}</th>
                                    <th>{t('propertyManagement.fields.type')}</th>
                                    <th>{t('propertyManagement.fields.phone')}</th>
                                    <th>{t('propertyManagement.fields.email')}</th>
                                    <th>{t('propertyManagement.fields.moveInDate')}</th>
                                    <th>{t('propertyManagement.fields.status')}</th>
                                    <th className="text-end">{t('propertyManagement.fields.actions')}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {residents.map((resident) => (
                                    <tr key={resident.id}>
                                        <td>{resident.siteName}</td>
                                        <td>{resident.unitNumber ? [resident.blockName, resident.unitNumber].filter(Boolean).join(' / ') : '-'}</td>
                                        <td className="fw-medium">{resident.fullName}</td>
                                        <td>{residentTypeLabel(resident.type, t)}</td>
                                        <td>{resident.phone || '-'}</td>
                                        <td>{resident.email || '-'}</td>
                                        <td>{formatDate(resident.moveInDate)}</td>
                                        <td>{statusBadge(resident.isActive, resident.isArchived, t)}</td>
                                        <td className="text-end">
                                            <div className="d-flex justify-content-end gap-1">
                                                <Button variant="default" size="sm" className="btn-icon" onClick={() => void openEditResident(resident.id)} title={t('common.edit')}><LuPencil className="fs-base" /></Button>
                                                <Button variant="default" size="sm" className="btn-icon text-danger" onClick={() => void archiveResident(resident.id)} title={t('common.delete')}><LuTrash2 className="fs-base" /></Button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </Table>
                    </div>

                    <CardFooter className="border-0">
                        <TablePagination
                            totalItems={residentTotal}
                            start={(residentPage - 1) * pageSize + 1}
                            end={Math.min(residentPage * pageSize, residentTotal)}
                            itemsName={t('propertyManagement.tabs.residents').toLowerCase()}
                            showInfo
                            previousPage={() => setResidentPage(Math.max(1, residentPage - 1))}
                            canPreviousPage={residentPage > 1}
                            pageCount={Math.ceil(residentTotal / pageSize)}
                            pageIndex={residentPage - 1}
                            setPageIndex={(idx) => setResidentPage(idx + 1)}
                            nextPage={() => setResidentPage(Math.min(Math.ceil(residentTotal / pageSize), residentPage + 1))}
                            canNextPage={residentPage < Math.ceil(residentTotal / pageSize)}
                        />
                    </CardFooter>
                </Card>
            </Container>

            <ResidentModal show={showResidentModal} onHide={() => setShowResidentModal(false)} form={residentForm} setForm={setResidentForm} sites={sites} units={units} onSave={saveResident} isEdit={!!editingResidentId} />

            <style>{`
                .app-search { position: relative; }
                .app-search .form-control { padding-left: 2.5rem; border-radius: 4px; }
                .app-search-icon { position: absolute; left: 12px; top: 50%; transform: translateY(-50%); font-size: 16px; }
                .btn-icon { width: 32px; height: 32px; padding: 0; display: flex; align-items: center; justify-content: center; transition: all 0.2s; background: transparent; border: 1px solid transparent; }
                .btn-icon:hover { background-color: var(--bs-tertiary-bg) !important; transform: translateY(-2px); box-shadow: var(--bs-box-shadow-sm) !important; border-color: var(--bs-border-color); }
                .table-custom tr:hover { background-color: rgba(var(--bs-primary-rgb), 0.05); }
            `}</style>
        </VerticalLayout>
    )
}

const ResidentModal = ({ show, onHide, form, setForm, sites, units, onSave, isEdit }: { show: boolean; onHide: () => void; form: ResidentPayload; setForm: (value: ResidentPayload) => void; sites: SiteListItem[]; units: UnitListItem[]; onSave: () => Promise<void>; isEdit: boolean }) => {
    const { t } = useTranslation()
    const filteredUnits = units.filter((unit) => unit.siteId === form.siteId)

    return (
        <Modal show={show} onHide={onHide} size="lg">
            <Modal.Header closeButton><Modal.Title>{t(isEdit ? 'propertyManagement.modals.editResident' : 'propertyManagement.modals.addResident')}</Modal.Title></Modal.Header>
            <Modal.Body>
                <Row className="g-3">
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.site')}</Form.Label><Form.Select value={form.siteId} disabled={isEdit} onChange={(e) => setForm({ ...form, siteId: e.target.value, unitId: null })}>{sites.map((site) => <option key={site.id} value={site.id}>{site.name}</option>)}</Form.Select></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.unit')}</Form.Label><Form.Select value={form.unitId ?? ''} onChange={(e) => setForm({ ...form, unitId: e.target.value || null })}><option value="">{t('propertyManagement.fields.noUnit')}</option>{filteredUnits.map((unit) => <option key={unit.id} value={unit.id}>{[unit.blockName, unit.number].filter(Boolean).join(' / ')}</option>)}</Form.Select></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.firstName')}</Form.Label><Form.Control value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} /></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.lastName')}</Form.Label><Form.Control value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} /></Col>
                    <Col md={4}><Form.Label>{t('propertyManagement.fields.type')}</Form.Label><Form.Select value={form.type} onChange={(e) => setForm({ ...form, type: Number(e.target.value) as ResidentType })}>{Object.values(ResidentType).filter((v) => typeof v === 'number').map((type) => <option key={type} value={type}>{residentTypeLabel(type as ResidentType, t)}</option>)}</Form.Select></Col>
                    <Col md={4}><Form.Label>{t('propertyManagement.fields.identityNumber')}</Form.Label><Form.Control value={form.identityNumber ?? ''} onChange={(e) => setForm({ ...form, identityNumber: e.target.value })} /></Col>
                    <Col md={4}><Form.Label>{t('propertyManagement.fields.occupation')}</Form.Label><Form.Control value={form.occupation ?? ''} onChange={(e) => setForm({ ...form, occupation: e.target.value })} /></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.phone')}</Form.Label><Form.Control value={form.phone ?? ''} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.email')}</Form.Label><Form.Control type="email" value={form.email ?? ''} onChange={(e) => setForm({ ...form, email: e.target.value })} /></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.emergencyContactName')}</Form.Label><Form.Control value={form.emergencyContactName ?? ''} onChange={(e) => setForm({ ...form, emergencyContactName: e.target.value })} /></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.emergencyContactPhone')}</Form.Label><Form.Control value={form.emergencyContactPhone ?? ''} onChange={(e) => setForm({ ...form, emergencyContactPhone: e.target.value })} /></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.moveInDate')}</Form.Label><Form.Control type="date" value={form.moveInDate ?? ''} onChange={(e) => setForm({ ...form, moveInDate: dateOrNull(e.target.value) })} /></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.moveOutDate')}</Form.Label><Form.Control type="date" value={form.moveOutDate ?? ''} onChange={(e) => setForm({ ...form, moveOutDate: dateOrNull(e.target.value) })} /></Col>
                    <Col md={12}><Form.Label>{t('propertyManagement.fields.notes')}</Form.Label><Form.Control as="textarea" rows={2} value={form.notes ?? ''} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></Col>
                    <Col md={4}><Form.Check type="switch" label={t('propertyManagement.fields.kvkkConsent')} checked={form.kvkkConsentGiven} onChange={(e) => setForm({ ...form, kvkkConsentGiven: e.target.checked })} /></Col>
                    <Col md={4}><Form.Check type="switch" label={t('propertyManagement.fields.communicationConsent')} checked={form.communicationConsentGiven} onChange={(e) => setForm({ ...form, communicationConsentGiven: e.target.checked })} /></Col>
                    <Col md={4}><Form.Check type="switch" label={t('propertyManagement.status.active')} checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} /></Col>
                </Row>
            </Modal.Body>
            <Modal.Footer><Button variant="light" onClick={onHide}>{t('common.cancel')}</Button><Button onClick={() => void onSave()}>{t('common.save')}</Button></Modal.Footer>
        </Modal>
    )
}

const mapResidentToPayload = (resident: ResidentDetail): ResidentPayload => ({
    siteId: resident.siteId,
    unitId: resident.unitId ?? null,
    firstName: resident.firstName,
    lastName: resident.lastName,
    identityNumber: resident.identityNumber ?? '',
    type: resident.type,
    phone: resident.phone ?? '',
    email: resident.email ?? '',
    occupation: resident.occupation ?? '',
    emergencyContactName: resident.emergencyContactName ?? '',
    emergencyContactPhone: resident.emergencyContactPhone ?? '',
    moveInDate: resident.moveInDate ?? '',
    moveOutDate: resident.moveOutDate ?? '',
    kvkkConsentGiven: resident.kvkkConsentGiven,
    communicationConsentGiven: resident.communicationConsentGiven,
    notes: resident.notes ?? '',
    isActive: resident.isActive,
})

const normalizeResidentPayload = (resident: ResidentPayload): ResidentPayload => ({
    ...resident,
    unitId: resident.unitId || null,
    moveInDate: resident.moveInDate || null,
    moveOutDate: resident.moveOutDate || null,
})

export default ResidentsPage
