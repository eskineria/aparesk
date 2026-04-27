import { useCallback, useEffect, useState } from 'react'
import { Badge, Button, Col, Container, Form, Modal, Row, Table, Card, CardHeader, CardFooter } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPencil, LuPlus, LuTrash2, LuSearch } from 'react-icons/lu'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import TablePagination from '@/components/table/TablePagination'
import PropertyManagementService from '@/services/propertyManagementService'
import { turkeyCities, turkeyLocations, type TurkeyCity } from '@/data/turkeyLocations'
import type {
    SiteDetail,
    SiteListItem,
    SitePayload,
} from '@/types/propertyManagement'
import { showToast } from '@/utils/toast'
import type { TFunction } from 'i18next'

const pageSize = 10

const emptySite: SitePayload = {
    name: '',
    taxNumber: '',
    taxOffice: '',
    phone: '',
    email: '',
    addressLine: '',
    district: '',
    city: '',
    postalCode: '',
    isActive: true,
}

const statusBadge = (isActive: boolean, isArchived: boolean, t: TFunction) => {
    if (isArchived) return <Badge bg="secondary-subtle text-secondary">{t('propertyManagement.status.archived')}</Badge>
    return <Badge bg={isActive ? 'success-subtle text-success' : 'warning-subtle text-warning'}>{t(isActive ? 'propertyManagement.status.active' : 'propertyManagement.status.inactive')}</Badge>
}

const normalizeLocationKey = (value?: string | null) =>
    (value ?? '')
        .trim()
        .replaceAll('İ', 'I')
        .replaceAll('ı', 'i')
        .normalize('NFD')
        .replace(/\p{Diacritic}/gu, '')
        .toLowerCase()

const isTurkeyCity = (value: string): value is TurkeyCity => Object.hasOwn(turkeyLocations, value)

const findCityOption = (value?: string | null) => {
    const normalized = normalizeLocationKey(value)
    return turkeyCities.find((city) => normalizeLocationKey(city) === normalized) ?? ''
}

const findDistrictOption = (city: string, value?: string | null) => {
    if (!isTurkeyCity(city)) return value ?? ''

    const normalized = normalizeLocationKey(value)
    return turkeyLocations[city].find((district) => normalizeLocationKey(district) === normalized) ?? ''
}

const getPhoneDigits = (value?: string | null) => {
    let digits = (value ?? '').replace(/\D/g, '')

    if (digits.startsWith('90')) digits = digits.slice(2)
    if (digits.startsWith('0')) digits = digits.slice(1)

    return digits.slice(0, 10)
}

const formatPhone = (value?: string | null) => {
    const digits = getPhoneDigits(value)
    if (!digits) return ''

    const areaCode = digits.slice(0, 3)
    const prefix = digits.slice(3, 6)
    const firstPart = digits.slice(6, 8)
    const secondPart = digits.slice(8, 10)

    let formatted = '+90'
    if (areaCode) formatted += ` (${areaCode}${areaCode.length === 3 ? ')' : ''}`
    if (prefix) formatted += ` ${prefix}`
    if (firstPart) formatted += ` ${firstPart}`
    if (secondPart) formatted += ` ${secondPart}`

    return formatted
}

const SitesPage = () => {
    const { t } = useTranslation()
    const [sites, setSites] = useState<SiteListItem[]>([])
    const [sitePage, setSitePage] = useState(1)
    const [siteTotal, setSiteTotal] = useState(0)
    const [siteSearch, setSiteSearch] = useState('')
    const [showSiteModal, setShowSiteModal] = useState(false)
    const [editingSiteId, setEditingSiteId] = useState<string | null>(null)
    const [siteForm, setSiteForm] = useState<SitePayload>(emptySite)

    const fetchSites = useCallback(async () => {
        const result = await PropertyManagementService.getSites({ pageNumber: sitePage, pageSize, searchTerm: siteSearch || undefined })
        setSites(result.items)
        setSiteTotal(result.totalCount)
    }, [sitePage, siteSearch])

    useEffect(() => { void fetchSites() }, [fetchSites])

    const openNewSite = () => {
        setEditingSiteId(null)
        setSiteForm(emptySite)
        setShowSiteModal(true)
    }

    const openEditSite = async (id: string) => {
        const site = await PropertyManagementService.getSite(id)
        if (!site) return
        setEditingSiteId(id)
        setSiteForm(mapSiteToPayload(site))
        setShowSiteModal(true)
    }

    const saveSite = async () => {
        if (editingSiteId) {
            await PropertyManagementService.updateSite(editingSiteId, siteForm)
            showToast(t('propertyManagement.messages.siteUpdated'), 'success')
        } else {
            await PropertyManagementService.createSite(siteForm)
            showToast(t('propertyManagement.messages.siteCreated'), 'success')
        }
        setShowSiteModal(false)
        await fetchSites()
    }

    const archiveSite = async (id: string) => {
        await PropertyManagementService.archiveSite(id)
        showToast(t('propertyManagement.messages.siteArchived'), 'success')
        await fetchSites()
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('propertyManagement.title')} subtitle={t('propertyManagement.tabs.sites')} />
            <Container fluid>
                <Card className="border-0 shadow-sm overflow-hidden mb-3">
                    <CardHeader className="border-bottom border-light p-3">
                        <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 w-100">
                            <div className="d-flex flex-wrap gap-2 align-items-center">
                                <div className="app-search" style={{ minWidth: '280px' }}>
                                    <input
                                        value={siteSearch}
                                        onChange={(e) => setSiteSearch(e.target.value)}
                                        onKeyDown={(e) => e.key === 'Enter' && fetchSites()}
                                        type="search"
                                        className="form-control"
                                        placeholder={t('common.search') + "..."}
                                    />
                                    <LuSearch className="app-search-icon text-muted" />
                                </div>
                                <Button variant="outline-primary" onClick={() => { setSitePage(1); void fetchSites() }}>{t('common.search')}</Button>
                            </div>
                            <div className="d-flex gap-2">
                                <Button variant="primary" className="px-3 shadow-sm" onClick={openNewSite}>
                                    <LuPlus className="me-1" /> {t('propertyManagement.actions.addSite')}
                                </Button>
                            </div>
                        </div>
                    </CardHeader>

                    <div className="table-responsive">
                        <Table hover className="table-custom table-centered mb-0">
                            <thead className="bg-light align-middle bg-opacity-25 thead-sm">
                                <tr className="text-uppercase fs-xxs">
                                    <th>{t('propertyManagement.fields.site')}</th>
                                    <th>{t('propertyManagement.fields.location')}</th>
                                    <th>{t('propertyManagement.fields.block')}</th>
                                    <th>{t('propertyManagement.fields.unit')}</th>
                                    <th>{t('propertyManagement.fields.status')}</th>
                                    <th className="text-end">{t('propertyManagement.fields.actions')}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {sites.map((site) => (
                                    <tr key={site.id}>
                                        <td className="fw-medium">{site.name}</td>
                                        <td>{[site.district, site.city].filter(Boolean).join(' / ') || '-'}</td>
                                        <td>{site.blockCount}</td>
                                        <td>{site.unitCount}</td>
                                        <td>{statusBadge(site.isActive, site.isArchived, t)}</td>
                                        <td className="text-end">
                                            <div className="d-flex justify-content-end gap-1">
                                                <Button variant="default" size="sm" className="btn-icon" onClick={() => void openEditSite(site.id)} title={t('common.edit')}><LuPencil className="fs-base" /></Button>
                                                <Button variant="default" size="sm" className="btn-icon text-danger" onClick={() => void archiveSite(site.id)} title={t('common.delete')}><LuTrash2 className="fs-base" /></Button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </Table>
                    </div>

                    <CardFooter className="border-0">
                        <TablePagination
                            totalItems={siteTotal}
                            start={(sitePage - 1) * pageSize + 1}
                            end={Math.min(sitePage * pageSize, siteTotal)}
                            itemsName={t('propertyManagement.tabs.sites').toLowerCase()}
                            showInfo
                            previousPage={() => setSitePage(Math.max(1, sitePage - 1))}
                            canPreviousPage={sitePage > 1}
                            pageCount={Math.ceil(siteTotal / pageSize)}
                            pageIndex={sitePage - 1}
                            setPageIndex={(idx) => setSitePage(idx + 1)}
                            nextPage={() => setSitePage(Math.min(Math.ceil(siteTotal / pageSize), sitePage + 1))}
                            canNextPage={sitePage < Math.ceil(siteTotal / pageSize)}
                        />
                    </CardFooter>
                </Card>
            </Container>

            <SiteModal show={showSiteModal} onHide={() => setShowSiteModal(false)} form={siteForm} setForm={setSiteForm} onSave={saveSite} isEdit={!!editingSiteId} />

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

const SiteModal = ({ show, onHide, form, setForm, onSave, isEdit }: { show: boolean; onHide: () => void; form: SitePayload; setForm: (value: SitePayload) => void; onSave: () => Promise<void>; isEdit: boolean }) => {
    const { t } = useTranslation()
    const districts = form.city && isTurkeyCity(form.city) ? turkeyLocations[form.city] : []

    return (
        <Modal show={show} onHide={onHide} size="lg">
            <Modal.Header closeButton><Modal.Title>{t(isEdit ? 'propertyManagement.modals.editSite' : 'propertyManagement.modals.addSite')}</Modal.Title></Modal.Header>
            <Modal.Body>
                <Row className="g-3">
                    <Col md={12}><Form.Label>{t('propertyManagement.fields.siteName')}</Form.Label><Form.Control value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></Col>
                    <Col md={6}>
                        <Form.Label>{t('propertyManagement.fields.city')}</Form.Label>
                        <Form.Select value={form.city ?? ''} onChange={(e) => setForm({ ...form, city: e.target.value, district: '' })}>
                            <option value="">{t('propertyManagement.fields.selectCity')}</option>
                            {turkeyCities.map((city) => <option key={city} value={city}>{city}</option>)}
                        </Form.Select>
                    </Col>
                    <Col md={6}>
                        <Form.Label>{t('propertyManagement.fields.district')}</Form.Label>
                        <Form.Select value={form.district ?? ''} disabled={!form.city} onChange={(e) => setForm({ ...form, district: e.target.value })}>
                            <option value="">{t('propertyManagement.fields.selectDistrict')}</option>
                            {districts.map((district) => <option key={district} value={district}>{district}</option>)}
                        </Form.Select>
                    </Col>
                    <Col md={12}><Form.Label>{t('propertyManagement.fields.address')}</Form.Label><Form.Control as="textarea" rows={2} value={form.addressLine ?? ''} onChange={(e) => setForm({ ...form, addressLine: e.target.value })} /></Col>
                    <Col md={4}>
                        <Form.Label>{t('propertyManagement.fields.phone')}</Form.Label>
                        <Form.Control
                            type="tel"
                            inputMode="tel"
                            placeholder="+90 (5__) ___ __ __"
                            value={form.phone ?? ''}
                            onChange={(e) => setForm({ ...form, phone: formatPhone(e.target.value) })}
                        />
                    </Col>
                    <Col md={4}><Form.Label>{t('propertyManagement.fields.email')}</Form.Label><Form.Control value={form.email ?? ''} onChange={(e) => setForm({ ...form, email: e.target.value })} /></Col>
                    <Col md={4} className="d-flex align-items-end"><Form.Check type="switch" label={t('propertyManagement.status.active')} checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} /></Col>
                </Row>
            </Modal.Body>
            <Modal.Footer><Button variant="light" onClick={onHide}>{t('common.cancel')}</Button><Button onClick={() => void onSave()}>{t('common.save')}</Button></Modal.Footer>
        </Modal>
    )
}

const mapSiteToPayload = (site: SiteDetail): SitePayload => {
    const city = findCityOption(site.city)

    return {
        name: site.name,
        taxNumber: site.taxNumber ?? '',
        taxOffice: site.taxOffice ?? '',
        phone: formatPhone(site.phone),
        email: site.email ?? '',
        addressLine: site.addressLine ?? '',
        district: findDistrictOption(city, site.district),
        city,
        postalCode: site.postalCode ?? '',
        isActive: site.isActive,
    }
}

export default SitesPage
