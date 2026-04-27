import { useCallback, useEffect, useState } from 'react'
import { Badge, Button, Col, Container, Form, Modal, Row, Table, Card, CardHeader, CardFooter } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPencil, LuPlus, LuTrash2, LuSearch } from 'react-icons/lu'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import TablePagination from '@/components/table/TablePagination'
import PropertyManagementService from '@/services/propertyManagementService'
import type {
    UnitDetail,
    UnitListItem,
    UnitPayload,
    SiteListItem,
    BlockListItem,
} from '@/types/propertyManagement'
import { UnitType } from '@/types/propertyManagement'
import { showToast } from '@/utils/toast'
import type { TFunction } from 'i18next'

const pageSize = 10
const lookupPageSize = 100

const emptyUnit: UnitPayload = {
    siteId: '',
    siteBlockId: null,
    number: '',
    doorNumber: '',
    type: UnitType.Apartment,
    floorNumber: null,
    grossAreaSquareMeters: null,
    netAreaSquareMeters: null,
    landShare: null,
    notes: '',
    isActive: true,
}

const statusBadge = (isActive: boolean, isArchived: boolean, t: TFunction) => {
    if (isArchived) return <Badge bg="secondary-subtle text-secondary">{t('propertyManagement.status.archived')}</Badge>
    return <Badge bg={isActive ? 'success-subtle text-success' : 'warning-subtle text-warning'}>{t(isActive ? 'propertyManagement.status.active' : 'propertyManagement.status.inactive')}</Badge>
}

const toNullableNumber = (value: string) => {
    if (value.trim() === '') return null
    const parsed = Number(value)
    return Number.isFinite(parsed) ? parsed : null
}

const unitTypeLabel = (type: UnitType, t: TFunction) => t(`propertyManagement.unitTypes.${type}`, {
    defaultValue: t('propertyManagement.unitTypes.99'),
})

const UnitsPage = () => {
    const { t } = useTranslation()
    const [units, setUnits] = useState<UnitListItem[]>([])
    const [sites, setSites] = useState<SiteListItem[]>([])
    const [blocks, setBlocks] = useState<BlockListItem[]>([])
    const [unitPage, setUnitPage] = useState(1)
    const [unitTotal, setUnitTotal] = useState(0)
    const [unitSearch, setUnitSearch] = useState('')
    const [showUnitModal, setShowUnitModal] = useState(false)
    const [editingUnitId, setEditingUnitId] = useState<string | null>(null)
    const [unitForm, setUnitForm] = useState<UnitPayload>(emptyUnit)

    const fetchUnits = useCallback(async () => {
        const result = await PropertyManagementService.getUnits({ pageNumber: unitPage, pageSize, searchTerm: unitSearch || undefined })
        setUnits(result.items)
        setUnitTotal(result.totalCount)
    }, [unitPage, unitSearch])

    const fetchSites = useCallback(async () => {
        try {
            const result = await PropertyManagementService.getSites({ pageNumber: 1, pageSize: lookupPageSize })
            setSites(result.items.filter(s => !s.isArchived))
        } catch (error) {
            console.error(error)
            showToast(t('common.error'), 'error')
        }
    }, [t])

    const fetchBlocks = useCallback(async () => {
        try {
            const result = await PropertyManagementService.getBlocks({ pageNumber: 1, pageSize: lookupPageSize })
            setBlocks(result.items.filter(b => !b.isArchived))
        } catch (error) {
            console.error(error)
            showToast(t('common.error'), 'error')
        }
    }, [t])

    useEffect(() => { void fetchUnits() }, [fetchUnits])
    useEffect(() => { void fetchSites() }, [fetchSites])
    useEffect(() => { void fetchBlocks() }, [fetchBlocks])

    const openNewUnit = () => {
        setEditingUnitId(null)
        setUnitForm({ ...emptyUnit, siteId: sites[0]?.id ?? '' })
        setShowUnitModal(true)
    }

    const openEditUnit = async (id: string) => {
        const unit = await PropertyManagementService.getUnit(id)
        if (!unit) return
        setEditingUnitId(id)
        setUnitForm(mapUnitToPayload(unit))
        setShowUnitModal(true)
    }

    const saveUnit = async () => {
        if (editingUnitId) {
            await PropertyManagementService.updateUnit(editingUnitId, {
                siteBlockId: unitForm.siteBlockId,
                number: unitForm.number,
                doorNumber: unitForm.doorNumber,
                type: unitForm.type,
                floorNumber: unitForm.floorNumber,
                grossAreaSquareMeters: unitForm.grossAreaSquareMeters,
                netAreaSquareMeters: unitForm.netAreaSquareMeters,
                landShare: unitForm.landShare,
                notes: unitForm.notes,
                isActive: unitForm.isActive,
            })
            showToast(t('propertyManagement.messages.unitUpdated'), 'success')
        } else {
            await PropertyManagementService.createUnit(unitForm)
            showToast(t('propertyManagement.messages.unitCreated'), 'success')
        }
        setShowUnitModal(false)
        await fetchUnits()
    }

    const archiveUnit = async (id: string) => {
        await PropertyManagementService.archiveUnit(id)
        showToast(t('propertyManagement.messages.unitArchived'), 'success')
        await fetchUnits()
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('propertyManagement.title')} subtitle={t('propertyManagement.tabs.units')} />
            <Container fluid>
                <Card className="border-0 shadow-sm overflow-hidden mb-3">
                    <CardHeader className="border-bottom border-light p-3">
                        <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 w-100">
                            <div className="d-flex flex-wrap gap-2 align-items-center">
                                <div className="app-search" style={{ minWidth: '280px' }}>
                                    <input
                                        value={unitSearch}
                                        onChange={(e) => setUnitSearch(e.target.value)}
                                        onKeyDown={(e) => e.key === 'Enter' && fetchUnits()}
                                        type="search"
                                        className="form-control"
                                        placeholder={t('common.search') + "..."}
                                    />
                                    <LuSearch className="app-search-icon text-muted" />
                                </div>
                                <Button variant="outline-primary" onClick={() => { setUnitPage(1); void fetchUnits() }}>{t('common.search')}</Button>
                            </div>
                            <Button variant="primary" className="px-3 shadow-sm" onClick={openNewUnit}>
                                <LuPlus className="me-1" /> {t('propertyManagement.actions.addUnit')}
                            </Button>
                        </div>
                    </CardHeader>

                    <div className="table-responsive">
                        <Table hover className="table-custom table-centered mb-0">
                            <thead className="bg-light align-middle bg-opacity-25 thead-sm">
                                <tr className="text-uppercase fs-xxs">
                                    <th>{t('propertyManagement.fields.site')}</th>
                                    <th>{t('propertyManagement.fields.block')}</th>
                                    <th>{t('propertyManagement.fields.no')}</th>
                                    <th>{t('propertyManagement.fields.type')}</th>
                                    <th>{t('propertyManagement.fields.floor')}</th>
                                    <th>{t('propertyManagement.fields.squareMeters')}</th>
                                    <th>{t('propertyManagement.fields.landShare')}</th>
                                    <th>{t('propertyManagement.fields.status')}</th>
                                    <th className="text-end">{t('propertyManagement.fields.actions')}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {units.map((unit) => (
                                    <tr key={unit.id}>
                                        <td>{unit.siteName}</td>
                                        <td>{unit.blockName || '-'}</td>
                                        <td className="fw-medium">{unit.number}</td>
                                        <td>{unitTypeLabel(unit.type, t)}</td>
                                        <td>{unit.floorNumber ?? '-'}</td>
                                        <td>{unit.netAreaSquareMeters ?? unit.grossAreaSquareMeters ?? '-'}</td>
                                        <td>{unit.landShare ?? '-'}</td>
                                        <td>{statusBadge(unit.isActive, unit.isArchived, t)}</td>
                                        <td className="text-end">
                                            <div className="d-flex justify-content-end gap-1">
                                                <Button variant="default" size="sm" className="btn-icon" onClick={() => void openEditUnit(unit.id)} title={t('common.edit')}><LuPencil className="fs-base" /></Button>
                                                <Button variant="default" size="sm" className="btn-icon text-danger" onClick={() => void archiveUnit(unit.id)} title={t('common.delete')}><LuTrash2 className="fs-base" /></Button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </Table>
                    </div>

                    <CardFooter className="border-0">
                        <TablePagination
                            totalItems={unitTotal}
                            start={(unitPage - 1) * pageSize + 1}
                            end={Math.min(unitPage * pageSize, unitTotal)}
                            itemsName={t('propertyManagement.tabs.units').toLowerCase()}
                            showInfo
                            previousPage={() => setUnitPage(Math.max(1, unitPage - 1))}
                            canPreviousPage={unitPage > 1}
                            pageCount={Math.ceil(unitTotal / pageSize)}
                            pageIndex={unitPage - 1}
                            setPageIndex={(idx) => setUnitPage(idx + 1)}
                            nextPage={() => setUnitPage(Math.min(Math.ceil(unitTotal / pageSize), unitPage + 1))}
                            canNextPage={unitPage < Math.ceil(unitTotal / pageSize)}
                        />
                    </CardFooter>
                </Card>
            </Container>

            <UnitModal show={showUnitModal} onHide={() => setShowUnitModal(false)} form={unitForm} setForm={setUnitForm} sites={sites} blocks={blocks} onSave={saveUnit} isEdit={!!editingUnitId} />

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

const UnitModal = ({ show, onHide, form, setForm, sites, blocks, onSave, isEdit }: { show: boolean; onHide: () => void; form: UnitPayload; setForm: (value: UnitPayload) => void; sites: SiteListItem[]; blocks: BlockListItem[]; onSave: () => Promise<void>; isEdit: boolean }) => {
    const { t } = useTranslation()
    const filteredBlocks = blocks.filter((block) => block.siteId === form.siteId)
    return (
        <Modal show={show} onHide={onHide} size="lg">
            <Modal.Header closeButton><Modal.Title>{t(isEdit ? 'propertyManagement.modals.editUnit' : 'propertyManagement.modals.addUnit')}</Modal.Title></Modal.Header>
            <Modal.Body>
                <Row className="g-3">
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.site')}</Form.Label><Form.Select value={form.siteId} disabled={isEdit} onChange={(e) => setForm({ ...form, siteId: e.target.value, siteBlockId: null })}>{sites.map((site) => <option key={site.id} value={site.id}>{site.name}</option>)}</Form.Select></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.block')}</Form.Label><Form.Select value={form.siteBlockId ?? ''} onChange={(e) => setForm({ ...form, siteBlockId: e.target.value || null })}><option value="">{t('propertyManagement.fields.noBlock')}</option>{filteredBlocks.map((block) => <option key={block.id} value={block.id}>{block.name}</option>)}</Form.Select></Col>
                    <Col md={3}><Form.Label>{t('propertyManagement.fields.unitNumber')}</Form.Label><Form.Control value={form.number} onChange={(e) => setForm({ ...form, number: e.target.value })} /></Col>
                    <Col md={3}><Form.Label>{t('propertyManagement.fields.doorNumber')}</Form.Label><Form.Control value={form.doorNumber ?? ''} onChange={(e) => setForm({ ...form, doorNumber: e.target.value })} /></Col>
                    <Col md={3}><Form.Label>{t('propertyManagement.fields.type')}</Form.Label><Form.Select value={form.type} onChange={(e) => setForm({ ...form, type: Number(e.target.value) as UnitType })}>{Object.values(UnitType).filter((v) => typeof v === 'number').map((type) => <option key={type} value={type}>{unitTypeLabel(type as UnitType, t)}</option>)}</Form.Select></Col>
                    <Col md={3}><Form.Label>{t('propertyManagement.fields.floor')}</Form.Label><Form.Control type="number" value={form.floorNumber ?? ''} onChange={(e) => setForm({ ...form, floorNumber: toNullableNumber(e.target.value) })} /></Col>
                    <Col md={4}><Form.Label>{t('propertyManagement.fields.grossArea')}</Form.Label><Form.Control type="number" value={form.grossAreaSquareMeters ?? ''} onChange={(e) => setForm({ ...form, grossAreaSquareMeters: toNullableNumber(e.target.value) })} /></Col>
                    <Col md={4}><Form.Label>{t('propertyManagement.fields.netArea')}</Form.Label><Form.Control type="number" value={form.netAreaSquareMeters ?? ''} onChange={(e) => setForm({ ...form, netAreaSquareMeters: toNullableNumber(e.target.value) })} /></Col>
                    <Col md={4}><Form.Label>{t('propertyManagement.fields.landShare')}</Form.Label><Form.Control type="number" value={form.landShare ?? ''} onChange={(e) => setForm({ ...form, landShare: toNullableNumber(e.target.value) })} /></Col>
                    <Col md={12}><Form.Label>{t('propertyManagement.fields.notes')}</Form.Label><Form.Control as="textarea" rows={2} value={form.notes ?? ''} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></Col>
                    <Col md={12}><Form.Check type="switch" label={t('propertyManagement.status.active')} checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} /></Col>
                </Row>
            </Modal.Body>
            <Modal.Footer><Button variant="light" onClick={onHide}>{t('common.cancel')}</Button><Button onClick={() => void onSave()}>{t('common.save')}</Button></Modal.Footer>
        </Modal>
    )
}

const mapUnitToPayload = (unit: UnitDetail): UnitPayload => ({
    siteId: unit.siteId,
    siteBlockId: unit.siteBlockId ?? null,
    number: unit.number,
    doorNumber: unit.doorNumber ?? '',
    type: unit.type,
    floorNumber: unit.floorNumber ?? null,
    grossAreaSquareMeters: unit.grossAreaSquareMeters ?? null,
    netAreaSquareMeters: unit.netAreaSquareMeters ?? null,
    landShare: unit.landShare ?? null,
    notes: unit.notes ?? '',
    isActive: unit.isActive,
})

export default UnitsPage
