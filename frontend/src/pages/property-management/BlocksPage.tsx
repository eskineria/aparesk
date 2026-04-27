import { useCallback, useEffect, useState } from 'react'
import { Badge, Button, Col, Container, Form, Modal, Row, Table, Card, CardHeader, CardFooter } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPencil, LuPlus, LuTrash2, LuSearch } from 'react-icons/lu'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import TablePagination from '@/components/table/TablePagination'
import PropertyManagementService from '@/services/propertyManagementService'
import type {
    BlockDetail,
    BlockListItem,
    BlockPayload,
    SiteListItem,
} from '@/types/propertyManagement'
import { showToast } from '@/utils/toast'
import type { TFunction } from 'i18next'

const pageSize = 10
const lookupPageSize = 100

const emptyBlock: BlockPayload = {
    siteId: '',
    name: '',
    floorCount: null,
    unitsPerFloor: null,
    description: '',
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

const BlocksPage = () => {
    const { t } = useTranslation()
    const [blocks, setBlocks] = useState<BlockListItem[]>([])
    const [sites, setSites] = useState<SiteListItem[]>([])
    const [blockPage, setBlockPage] = useState(1)
    const [blockTotal, setBlockTotal] = useState(0)
    const [blockSearch, setBlockSearch] = useState('')
    const [showBlockModal, setShowBlockModal] = useState(false)
    const [editingBlockId, setEditingBlockId] = useState<string | null>(null)
    const [blockForm, setBlockForm] = useState<BlockPayload>(emptyBlock)

    const fetchBlocks = useCallback(async () => {
        const result = await PropertyManagementService.getBlocks({ pageNumber: blockPage, pageSize, searchTerm: blockSearch || undefined })
        setBlocks(result.items)
        setBlockTotal(result.totalCount)
    }, [blockPage, blockSearch])

    const fetchSites = useCallback(async () => {
        try {
            const result = await PropertyManagementService.getSites({ pageNumber: 1, pageSize: lookupPageSize })
            const activeSites = result.items.filter(s => !s.isArchived)
            setSites(activeSites)
            return activeSites
        } catch (error) {
            console.error(error)
            showToast(t('common.error'), 'error')
            return []
        }
    }, [t])

    useEffect(() => { void fetchBlocks() }, [fetchBlocks])
    useEffect(() => { void fetchSites() }, [fetchSites])

    const openNewBlock = async () => {
        if (sites.length === 0) await fetchSites()
        setEditingBlockId(null)
        setBlockForm(emptyBlock)
        setShowBlockModal(true)
    }

    const openEditBlock = async (id: string) => {
        const block = await PropertyManagementService.getBlock(id)
        if (!block) return
        setEditingBlockId(id)
        setBlockForm(mapBlockToPayload(block))
        setShowBlockModal(true)
    }

    const saveBlock = async () => {
        try {
            if (editingBlockId) {
                await PropertyManagementService.updateBlock(editingBlockId, {
                    name: blockForm.name,
                    floorCount: blockForm.floorCount,
                    description: blockForm.description,
                    isActive: blockForm.isActive,
                })
                showToast(t('propertyManagement.messages.blockUpdated'), 'success')
            } else {
                if (!blockForm.siteId) {
                    showToast(t('propertyManagement.messages.selectSite'), 'error')
                    return
                }
                await PropertyManagementService.createBlock(blockForm)
                showToast(t('propertyManagement.messages.blockCreated'), 'success')
            }
            setShowBlockModal(false)
            await fetchBlocks()
        } catch (error) {
            console.error(error)
            showToast(t('common.error'), 'error')
        }
    }

    const archiveBlock = async (id: string) => {
        await PropertyManagementService.archiveBlock(id)
        showToast(t('propertyManagement.messages.blockArchived'), 'success')
        await fetchBlocks()
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('propertyManagement.title')} subtitle={t('propertyManagement.tabs.blocks')} />
            <Container fluid>
                <Card className="border-0 shadow-sm overflow-hidden mb-3">
                    <CardHeader className="border-bottom border-light p-3">
                        <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 w-100">
                            <div className="d-flex flex-wrap gap-2 align-items-center">
                                <div className="app-search" style={{ minWidth: '280px' }}>
                                    <input
                                        value={blockSearch}
                                        onChange={(e) => setBlockSearch(e.target.value)}
                                        onKeyDown={(e) => e.key === 'Enter' && fetchBlocks()}
                                        type="search"
                                        className="form-control"
                                        placeholder={t('common.search') + "..."}
                                    />
                                    <LuSearch className="app-search-icon text-muted" />
                                </div>
                                <Button variant="outline-primary" onClick={() => { setBlockPage(1); void fetchBlocks() }}>{t('common.search')}</Button>
                            </div>
                            <Button variant="primary" className="px-3 shadow-sm" onClick={() => void openNewBlock()}>
                                <LuPlus className="me-1" /> {t('propertyManagement.actions.addBlock')}
                            </Button>
                        </div>
                    </CardHeader>

                    <div className="table-responsive">
                        <Table hover className="table-custom table-centered mb-0">
                            <thead className="bg-light align-middle bg-opacity-25 thead-sm">
                                <tr className="text-uppercase fs-xxs">
                                    <th>{t('propertyManagement.fields.site')}</th>
                                    <th>{t('propertyManagement.fields.block')}</th>
                                    <th>{t('propertyManagement.fields.floor')}</th>
                                    <th>{t('propertyManagement.fields.unit')}</th>
                                    <th>{t('propertyManagement.fields.status')}</th>
                                    <th className="text-end">{t('propertyManagement.fields.actions')}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {blocks.map((block) => (
                                    <tr key={block.id}>
                                        <td>{block.siteName}</td>
                                        <td className="fw-medium">{block.name}</td>
                                        <td>{block.floorCount ?? '-'}</td>
                                        <td>{block.unitCount}</td>
                                        <td>{statusBadge(block.isActive, block.isArchived, t)}</td>
                                        <td className="text-end">
                                            <div className="d-flex justify-content-end gap-1">
                                                <Button variant="default" size="sm" className="btn-icon" onClick={() => void openEditBlock(block.id)} title={t('common.edit')}><LuPencil className="fs-base" /></Button>
                                                <Button variant="default" size="sm" className="btn-icon text-danger" onClick={() => void archiveBlock(block.id)} title={t('common.delete')}><LuTrash2 className="fs-base" /></Button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </Table>
                    </div>

                    <CardFooter className="border-0">
                        <TablePagination
                            totalItems={blockTotal}
                            start={(blockPage - 1) * pageSize + 1}
                            end={Math.min(blockPage * pageSize, blockTotal)}
                            itemsName={t('propertyManagement.tabs.blocks').toLowerCase()}
                            showInfo
                            previousPage={() => setBlockPage(Math.max(1, blockPage - 1))}
                            canPreviousPage={blockPage > 1}
                            pageCount={Math.ceil(blockTotal / pageSize)}
                            pageIndex={blockPage - 1}
                            setPageIndex={(idx) => setBlockPage(idx + 1)}
                            nextPage={() => setBlockPage(Math.min(Math.ceil(blockTotal / pageSize), blockPage + 1))}
                            canNextPage={blockPage < Math.ceil(blockTotal / pageSize)}
                        />
                    </CardFooter>
                </Card>
            </Container>

            <BlockModal show={showBlockModal} onHide={() => setShowBlockModal(false)} form={blockForm} setForm={setBlockForm} sites={sites} onSave={saveBlock} isEdit={!!editingBlockId} />

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

const BlockModal = ({ show, onHide, form, setForm, sites, onSave, isEdit }: { show: boolean; onHide: () => void; form: BlockPayload; setForm: (value: BlockPayload) => void; sites: SiteListItem[]; onSave: () => Promise<void>; isEdit: boolean }) => {
    const { t } = useTranslation()
    const canSave = isEdit || Boolean(form.siteId && form.floorCount && form.unitsPerFloor)

    return (
        <Modal show={show} onHide={onHide}>
            <Modal.Header closeButton><Modal.Title>{t(isEdit ? 'propertyManagement.modals.editBlock' : 'propertyManagement.modals.addBlock')}</Modal.Title></Modal.Header>
            <Modal.Body>
                <Row className="g-3">
                    <Col md={12}>
                        <Form.Label>{t('propertyManagement.fields.site')}</Form.Label>
                        <Form.Select value={form.siteId} disabled={isEdit || sites.length === 0} onChange={(e) => setForm({ ...form, siteId: e.target.value })}>
                            <option value="">{t(sites.length === 0 ? 'propertyManagement.messages.noSites' : 'propertyManagement.fields.selectSite')}</option>
                            {sites.map((site) => <option key={site.id} value={site.id}>{site.name}</option>)}
                        </Form.Select>
                    </Col>
                    <Col md={12}><Form.Label>{t('propertyManagement.fields.blockName')}</Form.Label><Form.Control value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></Col>
                    <Col md={6}><Form.Label>{t('propertyManagement.fields.floorCount')}</Form.Label><Form.Control min={1} type="number" value={form.floorCount ?? ''} onChange={(e) => setForm({ ...form, floorCount: toNullableNumber(e.target.value) })} /></Col>
                    {!isEdit && <Col md={6}><Form.Label>{t('propertyManagement.fields.unitsPerFloor')}</Form.Label><Form.Control min={1} type="number" value={form.unitsPerFloor ?? ''} onChange={(e) => setForm({ ...form, unitsPerFloor: toNullableNumber(e.target.value) })} /></Col>}
                    <Col md={12}><Form.Label>{t('propertyManagement.fields.description')}</Form.Label><Form.Control as="textarea" rows={2} value={form.description ?? ''} onChange={(e) => setForm({ ...form, description: e.target.value })} /></Col>
                    <Col md={12}><Form.Check type="switch" label={t('propertyManagement.status.active')} checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} /></Col>
                </Row>
            </Modal.Body>
            <Modal.Footer><Button variant="light" onClick={onHide}>{t('common.cancel')}</Button><Button disabled={!canSave} onClick={() => void onSave()}>{t('common.save')}</Button></Modal.Footer>
        </Modal>
    )
}

const mapBlockToPayload = (block: BlockDetail): BlockPayload => ({
    siteId: block.siteId,
    name: block.name,
    floorCount: block.floorCount ?? null,
    unitsPerFloor: null,
    description: block.description ?? '',
    isActive: block.isActive,
})

export default BlocksPage
