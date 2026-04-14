import DOMPurify from 'dompurify'
import { useEffect, useMemo, useState } from 'react'
import ReactQuill from 'react-quill'
import { Alert, Badge, Button, Card, Col, Container, Form, Row, Spinner, Table } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import DeleteConfirmationModal from '@/components/table/DeleteConfirmationModal'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import { useAuth } from '@/context/AuthContext'
import VerticalLayout from '@/layouts/VerticalLayout'
import { ComplianceService } from '@/services/complianceService'
import type { CreateTermsDto, TermsDto, UpdateTermsDto } from '@/types/compliance'
import { showToast } from '@/utils/toast'
import 'react-quill/dist/quill.snow.css'
import './ComplianceTerms.scss'

type TermsFormState = {
    type: string
    version: string
    summary: string
    effectiveDate: string
    content: string
    isActive: boolean
}

const COMMON_TYPES = ['TermsOfService', 'PrivacyPolicy', 'CookiePolicy']

const editorModules = {
    toolbar: [
        [{ header: [1, 2, 3, false] }],
        ['bold', 'italic', 'underline', 'strike'],
        [{ list: 'ordered' }, { list: 'bullet' }],
        ['blockquote', 'link'],
        [{ align: [] }],
        ['clean'],
    ],
}

const toDateTimeLocalValue = (iso: string | null | undefined) => {
    if (!iso) {
        return ''
    }

    const date = new Date(iso)
    if (Number.isNaN(date.getTime())) {
        return ''
    }

    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
    return local.toISOString().slice(0, 16)
}

const createDefaultForm = (): TermsFormState => ({
    type: 'TermsOfService',
    version: '',
    summary: '',
    effectiveDate: toDateTimeLocalValue(new Date().toISOString()),
    content: '',
    isActive: false,
})

const formatDateTime = (value: string, locale: string) => {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) {
        return value
    }

    return date.toLocaleString(locale)
}

const formatDate = (value: string, locale: string) => {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) {
        return value
    }

    return date.toLocaleDateString(locale)
}

const ComplianceTerms = () => {
    const { t, i18n } = useTranslation()
    const { permissions } = useAuth()

    const canManage = permissions.includes('Compliance.Manage')

    const [terms, setTerms] = useState<TermsDto[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [isSaving, setIsSaving] = useState(false)
    const [filterType, setFilterType] = useState('All')
    const [editingId, setEditingId] = useState<string | null>(null)
    const [previewId, setPreviewId] = useState<string | null>(null)
    const [deleteTarget, setDeleteTarget] = useState<TermsDto | null>(null)
    const [form, setForm] = useState<TermsFormState>(createDefaultForm)

    const tt = (key: string, fallback: string) => t(key, fallback)

    const loadTerms = async (nextPreviewId?: string | null) => {
        setIsLoading(true)
        try {
            const response = await ComplianceService.getAllTerms()
            if (response.success) {
                const items = response.data ?? []
                setTerms(items)

                if (nextPreviewId && items.some((item) => item.id === nextPreviewId)) {
                    setPreviewId(nextPreviewId)
                } else if (items.length > 0 && !previewId) {
                    setPreviewId(items[0].id)
                } else if (items.length === 0) {
                    setPreviewId(null)
                }
            }
        } catch {
            // Global axios interceptor handles toast.
        } finally {
            setIsLoading(false)
        }
    }

    useEffect(() => {
        void loadTerms()
    }, [])

    const availableTypes = useMemo(() => {
        const dynamicTypes = Array.from(new Set(terms.map((item) => item.type).filter(Boolean)))
        return Array.from(new Set([...COMMON_TYPES, ...dynamicTypes]))
    }, [terms])

    const filteredTerms = useMemo(() => {
        const items = filterType === 'All'
            ? terms
            : terms.filter((item) => item.type === filterType)

        return [...items].sort((left, right) => {
            if (left.type !== right.type) {
                return left.type.localeCompare(right.type)
            }

            const leftDate = new Date(left.effectiveDate).getTime()
            const rightDate = new Date(right.effectiveDate).getTime()
            return rightDate - leftDate
        })
    }, [filterType, terms])

    const editingTerm = useMemo(
        () => terms.find((item) => item.id === editingId) ?? null,
        [editingId, terms],
    )

    const previewTerm = useMemo(() => {
        if (previewId) {
            const selected = terms.find((item) => item.id === previewId)
            if (selected) {
                return selected
            }
        }

        return filteredTerms[0] ?? terms[0] ?? null
    }, [filteredTerms, previewId, terms])

    const resetForm = () => {
        setEditingId(null)
        setForm(createDefaultForm())
    }

    const startEdit = (term: TermsDto) => {
        setEditingId(term.id)
        setPreviewId(term.id)
        setForm({
            type: term.type,
            version: term.version,
            summary: term.summary ?? '',
            effectiveDate: toDateTimeLocalValue(term.effectiveDate),
            content: term.content,
            isActive: term.isActive,
        })
    }

    const validateForm = () => {
        if (!form.type.trim()) {
            showToast(tt('identity.compliance_terms.validation_type_required', 'Terms type is required.'), 'warning')
            return false
        }

        if (!editingTerm && !form.version.trim()) {
            showToast(tt('identity.compliance_terms.validation_version_required', 'Version is required.'), 'warning')
            return false
        }

        if (!editingTerm && !form.effectiveDate) {
            showToast(tt('identity.compliance_terms.validation_effective_date_required', 'Effective date is required.'), 'warning')
            return false
        }

        if (!form.content.trim()) {
            showToast(tt('identity.compliance_terms.validation_content_required', 'Terms content is required.'), 'warning')
            return false
        }

        return true
    }

    const handleSubmit = async () => {
        if (!canManage || !validateForm()) {
            return
        }

        setIsSaving(true)
        try {
            if (editingTerm) {
                const shouldActivate = form.isActive && !editingTerm.isActive
                const updatePayload: UpdateTermsDto = {
                    content: form.content.trim(),
                    summary: form.summary.trim() || null,
                    isActive: editingTerm.isActive ? form.isActive : false,
                }

                const updateResponse = await ComplianceService.updateTerms(editingTerm.id, updatePayload)
                if (updateResponse.success) {
                    if (shouldActivate) {
                        const activateResponse = await ComplianceService.activateTerms(editingTerm.id)
                        if (!activateResponse.success) {
                            return
                        }
                    }

                    showToast(
                        updateResponse.message || tt('identity.compliance_terms.update_success', 'Terms updated successfully.'),
                        'success',
                    )
                    resetForm()
                    await loadTerms(editingTerm.id)
                }
                return
            }

            const createPayload: CreateTermsDto = {
                type: form.type.trim(),
                version: form.version.trim(),
                content: form.content.trim(),
                summary: form.summary.trim() || null,
                effectiveDate: new Date(form.effectiveDate).toISOString(),
            }

            const createResponse = await ComplianceService.createTerms(createPayload)
            if (createResponse.success && createResponse.data) {
                const createdId = createResponse.data.id

                if (form.isActive) {
                    const activateResponse = await ComplianceService.activateTerms(createdId)
                    if (!activateResponse.success) {
                        return
                    }
                }

                showToast(
                    createResponse.message || tt('identity.compliance_terms.create_success', 'Terms created successfully.'),
                    'success',
                )
                resetForm()
                await loadTerms(createdId)
            }
        } catch {
            // Global axios interceptor handles toast.
        } finally {
            setIsSaving(false)
        }
    }

    const handleActivate = async (term: TermsDto) => {
        if (!canManage) {
            return
        }

        try {
            const response = await ComplianceService.activateTerms(term.id)
            if (response.success) {
                showToast(
                    response.message || tt('identity.compliance_terms.activate_success', 'Terms activated successfully.'),
                    'success',
                )
                await loadTerms(term.id)
            }
        } catch {
            // Global axios interceptor handles toast.
        }
    }

    const handleDelete = async () => {
        if (!canManage || !deleteTarget) {
            return
        }

        const target = deleteTarget
        setDeleteTarget(null)

        try {
            const response = await ComplianceService.deleteTerms(target.id)
            if (response.success) {
                showToast(
                    response.message || tt('identity.compliance_terms.delete_success', 'Terms deleted successfully.'),
                    'success',
                )

                if (editingId === target.id) {
                    resetForm()
                }

                const nextPreview = previewId === target.id ? null : previewId
                await loadTerms(nextPreview)
            }
        } catch {
            // Global axios interceptor handles toast.
        }
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title={tt('identity.compliance_terms.title', 'Compliance Terms')} subtitle={t('identity.title')} />
            <Container fluid>
                <Row className="g-3">
                    <Col xl={7}>
                        <Card className="mb-3">
                            <Card.Body>
                                <div className="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-3">
                                    <div>
                                        <h5 className="mb-1">{tt('identity.compliance_terms.list_title', 'Terms Library')}</h5>
                                        <p className="text-muted mb-0">
                                            {tt(
                                                'identity.compliance_terms.list_subtitle',
                                                'Manage published terms, privacy policy and cookie policy versions.',
                                            )}
                                        </p>
                                    </div>
                                    <div className="d-flex flex-wrap gap-2">
                                        <Button variant="light" onClick={() => void loadTerms(previewId)}>
                                            {tt('identity.compliance_terms.refresh', 'Refresh')}
                                        </Button>
                                        {canManage ? (
                                            <Button variant="primary" onClick={resetForm}>
                                                {tt('identity.compliance_terms.new_version', 'New version')}
                                            </Button>
                                        ) : null}
                                    </div>
                                </div>

                                <Row className="g-3 mb-3">
                                    <Col md={6} xl={5}>
                                        <Form.Group>
                                            <Form.Label htmlFor="terms-filter-type">
                                                {tt('identity.compliance_terms.filter_type', 'Type')}
                                            </Form.Label>
                                            <Form.Select
                                                id="terms-filter-type"
                                                value={filterType}
                                                onChange={(event) => setFilterType(event.target.value)}
                                            >
                                                <option value="All">{tt('identity.compliance_terms.all_types', 'All types')}</option>
                                                {availableTypes.map((type) => (
                                                    <option key={type} value={type}>
                                                        {type}
                                                    </option>
                                                ))}
                                            </Form.Select>
                                        </Form.Group>
                                    </Col>
                                </Row>

                                {!canManage ? (
                                    <Alert variant="light" className="border mb-3">
                                        {tt(
                                            'identity.compliance_terms.read_only_hint',
                                            'You have read-only access. Editing requires Compliance.Manage permission.',
                                        )}
                                    </Alert>
                                ) : null}

                                {isLoading ? (
                                    <div className="d-flex align-items-center gap-2 text-muted">
                                        <Spinner animation="border" size="sm" />
                                        <span>{t('common.loading')}</span>
                                    </div>
                                ) : filteredTerms.length === 0 ? (
                                    <div className="text-muted">{tt('identity.compliance_terms.no_terms', 'No terms versions found.')}</div>
                                ) : (
                                    <div className="table-responsive">
                                        <Table hover className="align-middle mb-0">
                                            <thead>
                                                <tr>
                                                    <th>{tt('identity.compliance_terms.summary', 'Summary')}</th>
                                                    <th>{tt('identity.compliance_terms.type', 'Type')}</th>
                                                    <th>{tt('identity.compliance_terms.version', 'Version')}</th>
                                                    <th>{tt('identity.compliance_terms.effective_date', 'Effective date')}</th>
                                                    <th>{tt('identity.compliance_terms.status', 'Status')}</th>
                                                    <th className="text-end">{tt('identity.compliance_terms.actions', 'Actions')}</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {filteredTerms.map((term) => (
                                                    <tr key={term.id}>
                                                        <td>
                                                            <button
                                                                type="button"
                                                                className="btn btn-link p-0 text-start text-decoration-none fw-semibold"
                                                                onClick={() => setPreviewId(term.id)}
                                                            >
                                                                {term.summary || tt('identity.compliance_terms.no_summary', 'No summary')}
                                                            </button>
                                                            <div className="small text-muted">
                                                                {tt('identity.compliance_terms.created_at', 'Created')}: {formatDateTime(term.createdAt, i18n.language)}
                                                            </div>
                                                        </td>
                                                        <td>{term.type}</td>
                                                        <td>{term.version}</td>
                                                        <td>{formatDate(term.effectiveDate, i18n.language)}</td>
                                                        <td>
                                                            <Badge bg={term.isActive ? 'success' : 'secondary'}>
                                                                {term.isActive
                                                                    ? tt('identity.compliance_terms.active', 'Active')
                                                                    : tt('identity.compliance_terms.inactive', 'Inactive')}
                                                            </Badge>
                                                        </td>
                                                        <td className="text-end">
                                                            <div className="d-flex justify-content-end gap-2 flex-wrap">
                                                                <Button variant="light" size="sm" onClick={() => setPreviewId(term.id)}>
                                                                    {tt('identity.compliance_terms.preview', 'Preview')}
                                                                </Button>
                                                                {canManage ? (
                                                                    <>
                                                                        <Button variant="light" size="sm" onClick={() => startEdit(term)}>
                                                                            {tt('identity.compliance_terms.edit', 'Edit')}
                                                                        </Button>
                                                                        {!term.isActive ? (
                                                                            <Button variant="outline-success" size="sm" onClick={() => void handleActivate(term)}>
                                                                                {tt('identity.compliance_terms.activate', 'Activate')}
                                                                            </Button>
                                                                        ) : null}
                                                                        <Button variant="outline-danger" size="sm" onClick={() => setDeleteTarget(term)}>
                                                                            {tt('identity.compliance_terms.delete', 'Delete')}
                                                                        </Button>
                                                                    </>
                                                                ) : null}
                                                            </div>
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </Table>
                                    </div>
                                )}
                            </Card.Body>
                        </Card>

                        <Card>
                            <Card.Body>
                                <h5 className="mb-1">{tt('identity.compliance_terms.preview_title', 'Preview')}</h5>
                                <p className="text-muted mb-3">
                                    {tt('identity.compliance_terms.preview_subtitle', 'Review the currently selected document before publishing or activating it.')}
                                </p>

                                {previewTerm ? (
                                    <>
                                        <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
                                            <Badge bg={previewTerm.isActive ? 'success' : 'secondary'}>
                                                {previewTerm.isActive
                                                    ? tt('identity.compliance_terms.active', 'Active')
                                                    : tt('identity.compliance_terms.inactive', 'Inactive')}
                                            </Badge>
                                            <span className="text-muted small">{previewTerm.type}</span>
                                            <span className="text-muted small">v{previewTerm.version}</span>
                                        </div>
                                        <h6>{previewTerm.summary || tt('identity.compliance_terms.no_summary', 'No summary')}</h6>
                                        <div className="text-muted small mb-3">
                                            {tt('identity.compliance_terms.effective_date', 'Effective date')}: {formatDateTime(previewTerm.effectiveDate, i18n.language)}
                                        </div>
                                        <div
                                            className="border rounded p-3 bg-light-subtle"
                                            style={{ maxHeight: 480, overflowY: 'auto' }}
                                            dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(previewTerm.content) }}
                                        />
                                    </>
                                ) : (
                                    <div className="text-muted">{tt('identity.compliance_terms.no_preview', 'Select a terms version to preview its contents.')}</div>
                                )}
                            </Card.Body>
                        </Card>
                    </Col>

                    <Col xl={5}>
                        <Card className="mb-3">
                            <Card.Body>
                                <div className="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-3">
                                    <div>
                                        <h5 className="mb-1">
                                            {editingTerm
                                                ? tt('identity.compliance_terms.form_edit_title', 'Edit version')
                                                : tt('identity.compliance_terms.form_create_title', 'Create version')}
                                        </h5>
                                        <p className="text-muted mb-0">
                                            {editingTerm
                                                ? tt(
                                                    'identity.compliance_terms.form_edit_subtitle',
                                                    'Content, summary and active state can be updated for existing versions.',
                                                )
                                                : tt(
                                                    'identity.compliance_terms.form_create_subtitle',
                                                    'Create a new legal document version and optionally activate it immediately.',
                                                )}
                                        </p>
                                    </div>
                                    {editingTerm ? (
                                        <Button variant="light" onClick={resetForm}>
                                            {tt('identity.compliance_terms.cancel_edit', 'Cancel edit')}
                                        </Button>
                                    ) : null}
                                </div>

                                {!canManage ? (
                                    <Alert variant="light" className="border mb-0">
                                        {tt(
                                            'identity.compliance_terms.form_locked',
                                            'This form is locked because your account does not have Compliance.Manage permission.',
                                        )}
                                    </Alert>
                                ) : (
                                    <Form>
                                        <Form.Group className="mb-3">
                                            <Form.Label htmlFor="terms-type">{tt('identity.compliance_terms.type', 'Type')}</Form.Label>
                                            <Form.Select
                                                id="terms-type"
                                                value={form.type}
                                                disabled={Boolean(editingTerm)}
                                                onChange={(event) => setForm((prev) => ({ ...prev, type: event.target.value }))}
                                            >
                                                {availableTypes.map((type) => (
                                                    <option key={type} value={type}>
                                                        {type}
                                                    </option>
                                                ))}
                                            </Form.Select>
                                        </Form.Group>

                                        <Row className="g-3">
                                            <Col md={6}>
                                                <Form.Group className="mb-3">
                                                    <Form.Label htmlFor="terms-version">{tt('identity.compliance_terms.version', 'Version')}</Form.Label>
                                                    <Form.Control
                                                        id="terms-version"
                                                        value={form.version}
                                                        disabled={Boolean(editingTerm)}
                                                        onChange={(event) => setForm((prev) => ({ ...prev, version: event.target.value }))}
                                                    />
                                                </Form.Group>
                                            </Col>
                                            <Col md={6}>
                                                <Form.Group className="mb-3">
                                                    <Form.Label htmlFor="terms-effective-date">
                                                        {tt('identity.compliance_terms.effective_date', 'Effective date')}
                                                    </Form.Label>
                                                    <Form.Control
                                                        id="terms-effective-date"
                                                        type="datetime-local"
                                                        value={form.effectiveDate}
                                                        disabled={Boolean(editingTerm)}
                                                        onChange={(event) => setForm((prev) => ({ ...prev, effectiveDate: event.target.value }))}
                                                    />
                                                </Form.Group>
                                            </Col>
                                        </Row>

                                        {editingTerm ? (
                                            <Alert variant="light" className="border">
                                                {tt(
                                                    'identity.compliance_terms.version_lock_hint',
                                                    'Type, version and effective date are locked after creation to preserve acceptance history.',
                                                )}
                                            </Alert>
                                        ) : null}

                                        <Form.Group className="mb-3">
                                            <Form.Label htmlFor="terms-summary">{tt('identity.compliance_terms.summary', 'Summary')}</Form.Label>
                                            <Form.Control
                                                id="terms-summary"
                                                value={form.summary}
                                                onChange={(event) => setForm((prev) => ({ ...prev, summary: event.target.value }))}
                                            />
                                        </Form.Group>

                                        <Form.Group className="mb-3">
                                            <Form.Label htmlFor="terms-content">{tt('identity.compliance_terms.content', 'Content')}</Form.Label>
                                            <div className="compliance-terms-editor">
                                                <ReactQuill
                                                    theme="snow"
                                                    modules={editorModules}
                                                    value={form.content}
                                                    onChange={(value) => setForm((prev) => ({ ...prev, content: value }))}
                                                />
                                            </div>
                                        </Form.Group>

                                        <Form.Check
                                            type="switch"
                                            id="terms-is-active"
                                            className="mb-3"
                                            label={tt('identity.compliance_terms.is_active', 'Activate this version')}
                                            checked={form.isActive}
                                            onChange={(event) => setForm((prev) => ({ ...prev, isActive: event.target.checked }))}
                                        />

                                        <div className="d-flex flex-wrap gap-2">
                                            <Button variant="primary" onClick={() => void handleSubmit()} disabled={isSaving}>
                                                {isSaving ? (
                                                    <>
                                                        <Spinner animation="border" size="sm" className="me-2" />
                                                        {t('common.loading')}
                                                    </>
                                                ) : editingTerm ? (
                                                    tt('identity.compliance_terms.update_button', 'Update version')
                                                ) : (
                                                    tt('identity.compliance_terms.create_button', 'Create version')
                                                )}
                                            </Button>
                                            {editingTerm ? (
                                                <Button variant="light" onClick={resetForm} disabled={isSaving}>
                                                    {tt('identity.compliance_terms.cancel_edit', 'Cancel edit')}
                                                </Button>
                                            ) : null}
                                        </div>
                                    </Form>
                                )}
                            </Card.Body>
                        </Card>
                    </Col>
                </Row>
            </Container>

            <DeleteConfirmationModal
                show={Boolean(deleteTarget)}
                onHide={() => setDeleteTarget(null)}
                onConfirm={() => void handleDelete()}
                selectedCount={deleteTarget ? 1 : 0}
                itemName={tt('identity.compliance_terms.item_name', 'terms version')}
                modalTitle={tt('identity.compliance_terms.delete_modal_title', 'Delete version')}
                confirmButtonText={tt('identity.compliance_terms.delete', 'Delete')}
                cancelButtonText={tt('common.cancel', 'Cancel')}
            >
                {deleteTarget
                    ? t(
                        'identity.compliance_terms.delete_confirm',
                        {
                            name: deleteTarget.summary || deleteTarget.type,
                            version: deleteTarget.version,
                            defaultValue: 'Delete "{{name}}" version {{version}}?',
                        },
                    )
                    : null}
            </DeleteConfirmationModal>
        </VerticalLayout>
    )
}

export default ComplianceTerms
