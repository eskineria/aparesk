import { useEffect, useState } from 'react'
import { Alert, Button, Card, CardFooter, CardHeader, Container, Form, Modal, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import {
    type ColumnDef,
    createColumnHelper,
    getCoreRowModel,
    getPaginationRowModel,
    getSortedRowModel,
    type SortingState,
    type Row as TableRow,
    useReactTable,
} from '@tanstack/react-table'
import {
    LuSearch,
} from 'react-icons/lu'
import { TbEye } from 'react-icons/tb'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import DataTable from '@/components/table/DataTable'
import TablePagination from '@/components/table/TablePagination'
import AuditLogService from '@/services/auditLogService'
import type {
    AuditDiffResult,
    AuditIntegritySummary,
    AuditLogFilterOptions,
    AuditLogItem,
} from '@/types/auditLog'
import { showToast } from '@/utils/toast'

const columnHelper = createColumnHelper<AuditLogItem>()

const formatDateTime = (value: string, locale: string) => {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) {
        return value
    }

    return date.toLocaleString(locale)
}

const hasException = (value: string | null) => {
    return value != null && value.trim().length > 0
}


const AuditLogs = () => {
    const { t, i18n } = useTranslation()

    const [logs, setLogs] = useState<AuditLogItem[]>([])
    const [totalCount, setTotalCount] = useState(0)
    const [pageNumber, setPageNumber] = useState(1)
    const [pageSize, setPageSize] = useState(20)
    const [logIdFilter, setLogIdFilter] = useState('')
    const [searchTerm, setSearchTerm] = useState('')
    const [serviceName, setServiceName] = useState('')
    const [methodName, setMethodName] = useState('')
    const [onlyErrors, setOnlyErrors] = useState(false)
    const [filterOptions, setFilterOptions] = useState<AuditLogFilterOptions>({ services: [], methods: [] })
    const [selectedLog, setSelectedLog] = useState<AuditLogItem | null>(null)
    const [isLoading, setIsLoading] = useState(false)
    const [integritySummary, setIntegritySummary] = useState<AuditIntegritySummary | null>(null)
    const [isLoadingIntegrity, setIsLoadingIntegrity] = useState(false)
    const [diffResult, setDiffResult] = useState<AuditDiffResult | null>(null)
    const [isLoadingDiff, setIsLoadingDiff] = useState(false)

    const columns: ColumnDef<AuditLogItem, any>[] = [
        columnHelper.accessor('executionTime', {
            header: t('identity.audit_logs.execution_time'),
            cell: ({ row }) => (
                <div className="d-flex flex-column">
                    <span className="fw-semibold">
                        {formatDateTime(row.original.executionTime, i18n.resolvedLanguage || i18n.language || 'en-US')}
                    </span>
                    <span className="text-muted fs-xs">#{row.original.id}</span>
                </div>
            ),
        }),
        columnHelper.accessor('serviceName', {
            header: t('identity.audit_logs.operation'),
            cell: ({ row }) => (
                <div className="d-flex flex-column gap-1">
                    <span className="badge bg-primary-subtle text-primary align-self-start">
                        {row.original.serviceName}
                    </span>
                    <span className="text-muted fs-xs">{row.original.methodName}</span>
                </div>
            ),
        }),
        columnHelper.accessor('userId', {
            header: t('identity.audit_logs.user'),
            cell: ({ row }) => (
                <span className="text-muted">{row.original.userId || '-'}</span>
            ),
        }),
        columnHelper.accessor('clientIpAddress', {
            header: t('identity.audit_logs.client_ip'),
            cell: ({ row }) => (
                <span>{row.original.clientIpAddress || '-'}</span>
            ),
        }),
        columnHelper.accessor('executionDuration', {
            header: t('identity.audit_logs.duration_ms'),
            cell: ({ row }) => (
                <span className="fw-semibold">{row.original.executionDuration} ms</span>
            ),
        }),
        {
            id: 'status',
            header: t('identity.audit_logs.status'),
            cell: ({ row }: { row: TableRow<AuditLogItem> }) => (
                hasException(row.original.exception) ? (
                    <span className="badge bg-danger-subtle text-danger">
                        {t('identity.audit_logs.status_error')}
                    </span>
                ) : (
                    <span className="badge bg-success-subtle text-success">
                        {t('identity.audit_logs.status_success')}
                    </span>
                )
            ),
        },
        {
            id: 'actions',
            header: t('identity.audit_logs.actions'),
            cell: ({ row }: { row: TableRow<AuditLogItem> }) => (
                <Button
                    variant="default"
                    size="sm"
                    className="btn-icon"
                    onClick={() => setSelectedLog(row.original)}
                    title={t('identity.audit_logs.details')}>
                    <TbEye className="fs-lg" />
                </Button>
            ),
        },
    ]

    const [sorting, setSorting] = useState<SortingState>([])
    const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 20 })

    const table = useReactTable({
        data: logs,
        columns,
        state: { sorting, pagination },
        onSortingChange: setSorting,
        onPaginationChange: setPagination,
        getCoreRowModel: getCoreRowModel(),
        getSortedRowModel: getSortedRowModel(),
        getPaginationRowModel: getPaginationRowModel(),
        manualPagination: true,
        pageCount: Math.ceil(totalCount / pageSize),
    })

    const fetchData = async () => {
        const normalizedLogId = Number.parseInt(logIdFilter.trim(), 10)
        const idFilter = Number.isNaN(normalizedLogId) || normalizedLogId <= 0
            ? undefined
            : normalizedLogId

        setIsLoading(true)
        try {
            const response = await AuditLogService.getPaged({
                id: idFilter,
                pageNumber,
                pageSize,
                searchTerm: searchTerm.trim() || undefined,
                serviceName: serviceName || undefined,
                methodName: methodName || undefined,
                onlyErrors,
            })
            // @ts-expect-error matching backend PagedResponse structure, not the interface
            setLogs(response.data ?? [])
            // @ts-expect-error matching backend
            setTotalCount(response.count ?? 0)
        } catch {
            showToast(t('common.error'), 'error')
        } finally {
            setIsLoading(false)
        }
    }

    const fetchFilterOptions = async () => {
        try {
            const response = await AuditLogService.getFilterOptions()
            setFilterOptions({
                services: response.services ?? [],
                methods: response.methods ?? [],
            })
        } catch {
            // Axios interceptor handles toast.
        }
    }


    const fetchIntegrity = async () => {
        setIsLoadingIntegrity(true)
        try {
            const response = await AuditLogService.getIntegrity()
            setIntegritySummary({
                ...response,
                brokenSampleAuditLogIds: response.brokenSampleAuditLogIds ?? [],
                missingSampleAuditLogIds: response.missingSampleAuditLogIds ?? [],
            })
        } catch {
            // Axios interceptor handles toast.
        } finally {
            setIsLoadingIntegrity(false)
        }
    }


    const getDiffSourceLabel = (source: AuditDiffResult['source'] | string) => {
        const key = `identity.audit_logs.diff_source_values.${source}`
        const localized = t(key)
        return localized === key ? source : localized
    }

    const getHardeningState = (summary: AuditIntegritySummary | null) => {
        if (!summary) {
            return { key: 'disabled', variant: 'secondary' as const }
        }

        if (!summary.featureEnabled) {
            return { key: 'disabled', variant: 'secondary' as const }
        }

        if (!summary.integrityTableExists) {
            return { key: 'table_missing', variant: 'warning' as const }
        }

        if (summary.brokenChainCount > 0) {
            return { key: 'broken', variant: 'danger' as const }
        }

        if (summary.missingHardeningCount > 0) {
            return { key: 'partial', variant: 'warning' as const }
        }

        return { key: 'healthy', variant: 'success' as const }
    }

    useEffect(() => {
        void fetchFilterOptions()
        void fetchIntegrity()
    }, [])

    useEffect(() => {
        const intervalId = window.setInterval(() => {
            void fetchIntegrity()
        }, 60000)

        return () => window.clearInterval(intervalId)
    }, [])

    useEffect(() => {
        const timer = window.setTimeout(() => {
            void fetchData()
        }, 350)

        return () => window.clearTimeout(timer)
    }, [pageNumber, pageSize, logIdFilter, searchTerm, serviceName, methodName, onlyErrors])

    useEffect(() => {
        if (!selectedLog) {
            setDiffResult(null)
            setIsLoadingDiff(false)
            return
        }

        let isActive = true
        setIsLoadingDiff(true)
        AuditLogService.getDiff(selectedLog.id)
            .then((response) => {
                if (!isActive) {
                    return
                }

                setDiffResult(response)
            })
            .catch(() => {
                if (!isActive) {
                    return
                }

                setDiffResult(null)
            })
            .finally(() => {
                if (isActive) {
                    setIsLoadingDiff(false)
                }
            })

        return () => {
            isActive = false
        }
    }, [selectedLog])

    const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1
    const endItem = Math.min(pageNumber * pageSize, totalCount)

    const resetFilters = () => {
        setLogIdFilter('')
        setSearchTerm('')
        setServiceName('')
        setMethodName('')
        setOnlyErrors(false)
        setPageNumber(1)
    }

    const applyLogIdFilter = (id: number) => {
        setLogIdFilter(String(id))
        setPageNumber(1)
    }

    const openSampleLogDetails = async (id: number) => {
        applyLogIdFilter(id)

        try {
            const response = await AuditLogService.getPaged({
                id,
                pageNumber: 1,
                pageSize: 1,
            })

            // @ts-expect-error matching backend
            const logItem = response.data?.[0]
            if (!logItem) {
                showToast(
                    t('identity.audit_logs.id_not_found'),
                    'warning',
                )
                return
            }

            setSelectedLog(logItem)
        } catch {
            showToast(t('common.error'), 'error')
        }
    }

    const hardeningState = getHardeningState(integritySummary)
    const brokenSampleIds = integritySummary?.brokenSampleAuditLogIds ?? []
    const missingSampleIds = integritySummary?.missingSampleAuditLogIds ?? []

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('identity.audit_logs.title')} subtitle={t('identity.title')} />
            <Container fluid>
                <Card className="mb-3">
                    <CardHeader className="border-light d-flex flex-wrap align-items-center justify-content-between gap-2">
                        <h5 className="mb-0">{t('identity.audit_logs.hardening_title')}</h5>
                    </CardHeader>
                    <Card.Body>
                        {isLoadingIntegrity ? (
                            <div className="d-flex align-items-center gap-2 text-muted">
                                <Spinner animation="border" size="sm" />
                                <span>{t('common.loading')}</span>
                            </div>
                        ) : integritySummary ? (
                            <div className="d-flex flex-column gap-2">
                                <div className="d-flex align-items-center justify-content-between">
                                    <span className="text-muted">{t('identity.audit_logs.hardening_status')}</span>
                                    <span className={`badge bg-${hardeningState.variant}-subtle text-${hardeningState.variant}`}>
                                        {t(`identity.audit_logs.hardening_status_values.${hardeningState.key}`)}
                                    </span>
                                </div>
                                <div className="row g-2">
                                    <div className="col-md-6">
                                        <div className="border rounded p-2 h-100">
                                            <div className="text-muted fs-xs">{t('identity.audit_logs.hardening_total_logs')}</div>
                                            <div className="fw-semibold">{integritySummary.totalAuditLogCount}</div>
                                        </div>
                                    </div>
                                    <div className="col-md-6">
                                        <div className="border rounded p-2 h-100">
                                            <div className="text-muted fs-xs">{t('identity.audit_logs.hardening_hardened_logs')}</div>
                                            <div className="fw-semibold">{integritySummary.hardenedLogCount}</div>
                                        </div>
                                    </div>
                                    <div className="col-md-6">
                                        <div className="border rounded p-2 h-100">
                                            <div className="text-muted fs-xs">{t('identity.audit_logs.hardening_missing_logs')}</div>
                                            <div className="fw-semibold">{integritySummary.missingHardeningCount}</div>
                                        </div>
                                    </div>
                                    <div className="col-md-6">
                                        <div className="border rounded p-2 h-100">
                                            <div className="text-muted fs-xs">{t('identity.audit_logs.hardening_broken_logs')}</div>
                                            <div className="fw-semibold">{integritySummary.brokenChainCount}</div>
                                        </div>
                                    </div>
                                </div>
                                <div className="d-flex flex-wrap gap-3 text-muted fs-xs">
                                    <span>
                                        {t('identity.audit_logs.hardening_last_hardened_id')}:{' '}
                                        {integritySummary.lastHardenedAuditLogId ?? '-'}
                                    </span>
                                </div>
                                <div className="d-flex flex-wrap align-items-center gap-2">
                                    <span className="text-muted fs-xs">
                                        {t('identity.audit_logs.hardening_broken_samples')}:
                                    </span>
                                    {brokenSampleIds.length > 0 ? (
                                        brokenSampleIds.map((id) => (
                                            <Button
                                                key={`broken-sample-${id}`}
                                                variant="outline-danger"
                                                size="sm"
                                                className="py-0 px-2"
                                                onClick={() => void openSampleLogDetails(id)}>
                                                #{id}
                                            </Button>
                                        ))
                                    ) : (
                                        <span className="text-muted fs-xs">-</span>
                                    )}
                                </div>
                                <div className="d-flex flex-wrap align-items-center gap-2">
                                    <span className="text-muted fs-xs">
                                        {t('identity.audit_logs.hardening_missing_samples')}:
                                    </span>
                                    {missingSampleIds.length > 0 ? (
                                        missingSampleIds.map((id) => (
                                            <Button
                                                key={`missing-sample-${id}`}
                                                variant="outline-warning"
                                                size="sm"
                                                className="py-0 px-2"
                                                onClick={() => void openSampleLogDetails(id)}>
                                                #{id}
                                            </Button>
                                        ))
                                    ) : (
                                        <span className="text-muted fs-xs">-</span>
                                    )}
                                </div>
                                {!integritySummary.featureEnabled && (
                                    <Alert variant="secondary" className="mb-0 py-2">
                                        {t('identity.audit_logs.hardening_disabled_hint')}
                                    </Alert>
                                )}
                                {integritySummary.featureEnabled && !integritySummary.integrityTableExists && (
                                    <Alert variant="warning" className="mb-0 py-2">
                                        {t('identity.audit_logs.hardening_table_missing_hint')}
                                    </Alert>
                                )}
                            </div>
                        ) : (
                            <Alert variant="secondary" className="mb-0 py-2">
                                {t('identity.audit_logs.hardening_unavailable')}
                            </Alert>
                        )}
                    </Card.Body>
                </Card>


                <Card>
                    <CardHeader className="border-light d-flex flex-wrap align-items-center justify-content-between gap-2">
                        <div className="d-flex flex-wrap gap-2 align-items-center">
                            <div className="app-search" style={{ minWidth: 260 }}>
                                <input
                                    type="search"
                                    className="form-control"
                                    value={searchTerm}
                                    onChange={(event) => {
                                        setSearchTerm(event.target.value)
                                        setPageNumber(1)
                                    }}
                                    placeholder={t('identity.audit_logs.search_placeholder')}
                                />
                                <LuSearch className="app-search-icon text-muted" />
                            </div>

                            <Form.Control
                                type="number"
                                min={1}
                                value={logIdFilter}
                                onChange={(event) => {
                                    setLogIdFilter(event.target.value)
                                    setPageNumber(1)
                                }}
                                placeholder={t('identity.audit_logs.log_id_placeholder')}
                                className="w-auto"
                            />

                            <Form.Select
                                value={serviceName}
                                onChange={(event) => {
                                    setServiceName(event.target.value)
                                    setPageNumber(1)
                                }}
                                className="w-auto">
                                <option value="">{t('identity.audit_logs.all_services')}</option>
                                {filterOptions.services.map((item) => (
                                    <option key={item} value={item}>
                                        {item}
                                    </option>
                                ))}
                            </Form.Select>

                            <Form.Select
                                value={methodName}
                                onChange={(event) => {
                                    setMethodName(event.target.value)
                                    setPageNumber(1)
                                }}
                                className="w-auto">
                                <option value="">{t('identity.audit_logs.all_methods')}</option>
                                {filterOptions.methods.map((item) => (
                                    <option key={item} value={item}>
                                        {item}
                                    </option>
                                ))}
                            </Form.Select>

                            <Form.Check
                                type="switch"
                                id="audit-logs-only-errors"
                                checked={onlyErrors}
                                label={t('identity.audit_logs.only_errors')}
                                onChange={(event) => {
                                    setOnlyErrors(event.target.checked)
                                    setPageNumber(1)
                                }}
                            />
                        </div>

                        <div className="d-flex align-items-center gap-2">
                            <span className="fw-semibold text-nowrap">{t('identity.table.show')}:</span>
                            <Form.Select
                                value={pageSize}
                                onChange={(event) => {
                                    setPageSize(Number(event.target.value))
                                    setPageNumber(1)
                                }}
                                className="w-auto">
                                {[10, 20, 50, 100].map((size) => (
                                    <option key={size} value={size}>
                                        {size}
                                    </option>
                                ))}
                            </Form.Select>

                            <Button variant="outline-secondary" onClick={resetFilters}>
                                {t('identity.audit_logs.reset_filters')}
                            </Button>
                        </div>
                    </CardHeader>

                    {isLoading ? (
                        <div className="p-4 d-flex align-items-center justify-content-center gap-2 text-muted">
                            <Spinner animation="border" size="sm" />
                            <span>{t('common.loading')}</span>
                        </div>
                    ) : (
                        <DataTable<AuditLogItem>
                            table={table}
                            emptyMessage={t('identity.audit_logs.no_logs')}
                        />
                    )}

                    {totalCount > 0 && (
                        <CardFooter className="border-0">
                            <TablePagination
                                totalItems={totalCount}
                                start={startItem}
                                end={endItem}
                                itemsName={t('identity.audit_logs.logs').toLowerCase()}
                                showInfo={true}
                                previousPage={() => setPageNumber((current) => Math.max(1, current - 1))}
                                canPreviousPage={pageNumber > 1}
                                pageCount={Math.ceil(totalCount / pageSize)}
                                pageIndex={pageNumber - 1}
                                setPageIndex={(index) => setPageNumber(index + 1)}
                                nextPage={() => setPageNumber((current) => Math.min(Math.ceil(totalCount / pageSize), current + 1))}
                                canNextPage={pageNumber < Math.ceil(totalCount / pageSize)}
                            />
                        </CardFooter>
                    )}
                </Card>
            </Container>

            <Modal show={selectedLog != null} onHide={() => setSelectedLog(null)} size="lg">
                <Modal.Header closeButton>
                    <Modal.Title>{t('identity.audit_logs.details_title')}</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    {selectedLog && (
                        <div className="d-flex flex-column gap-3">
                            <div>
                                <div className="fw-semibold">{t('identity.audit_logs.execution_time')}</div>
                                <div>{formatDateTime(selectedLog.executionTime, i18n.resolvedLanguage || i18n.language || 'en-US')}</div>
                            </div>

                            <div>
                                <div className="fw-semibold">{t('identity.audit_logs.operation')}</div>
                                <div>
                                    {selectedLog.serviceName}.{selectedLog.methodName}
                                </div>
                            </div>

                            <div>
                                <div className="fw-semibold">{t('identity.audit_logs.user')}</div>
                                <div>{selectedLog.userId || '-'}</div>
                            </div>

                            <div>
                                <div className="fw-semibold">{t('identity.audit_logs.client_ip')}</div>
                                <div>{selectedLog.clientIpAddress || '-'}</div>
                            </div>

                            <div>
                                <div className="fw-semibold">{t('identity.audit_logs.duration_ms')}</div>
                                <div>{selectedLog.executionDuration} ms</div>
                            </div>

                            <div>
                                <div className="fw-semibold">{t('identity.audit_logs.parameters')}</div>
                                <pre className="p-2 bg-light border rounded mb-0" style={{ whiteSpace: 'pre-wrap' }}>
                                    {selectedLog.parameters || '-'}
                                </pre>
                            </div>

                            <div>
                                <div className="fw-semibold mb-2">{t('identity.audit_logs.diff_title')}</div>
                                {isLoadingDiff ? (
                                    <div className="d-flex align-items-center gap-2 text-muted">
                                        <Spinner animation="border" size="sm" />
                                        <span>{t('identity.audit_logs.diff_loading')}</span>
                                    </div>
                                ) : diffResult?.source === 'disabled' ? (
                                    <Alert variant="warning" className="mb-0 py-2">
                                        {t('identity.audit_logs.diff_disabled')}
                                    </Alert>
                                ) : diffResult?.changes?.length ? (
                                    <div className="d-flex flex-column gap-2">
                                        <div className="text-muted fs-xs">
                                            {t('identity.audit_logs.diff_source')}: {getDiffSourceLabel(diffResult.source)}
                                            {diffResult.comparedLogId != null && ` | ${t('identity.audit_logs.diff_compared_log')}: #${diffResult.comparedLogId}`}
                                        </div>
                                        {!diffResult.hasComparableData && (
                                            <Alert variant="info" className="mb-0 py-2">
                                                {t('identity.audit_logs.diff_not_comparable')}
                                            </Alert>
                                        )}
                                        <div className="table-responsive border rounded">
                                            <table className="table table-sm align-middle mb-0">
                                                <thead className="table-light">
                                                    <tr>
                                                        <th>{t('identity.audit_logs.diff_field')}</th>
                                                        <th>{t('identity.audit_logs.diff_before')}</th>
                                                        <th>{t('identity.audit_logs.diff_after')}</th>
                                                        <th className="text-nowrap">{t('identity.audit_logs.diff_status')}</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    {diffResult.changes.map((change) => (
                                                        <tr key={change.field}>
                                                            <td className="fw-semibold">{change.field}</td>
                                                            <td className="text-muted" style={{ whiteSpace: 'pre-wrap' }}>{change.before || '-'}</td>
                                                            <td style={{ whiteSpace: 'pre-wrap' }}>{change.after || '-'}</td>
                                                            <td>
                                                                {change.changed ? (
                                                                    <span className="badge bg-warning-subtle text-warning">
                                                                        {t('identity.audit_logs.diff_changed')}
                                                                    </span>
                                                                ) : (
                                                                    <span className="badge bg-success-subtle text-success">
                                                                        {t('identity.audit_logs.diff_unchanged')}
                                                                    </span>
                                                                )}
                                                            </td>
                                                        </tr>
                                                    ))}
                                                </tbody>
                                            </table>
                                        </div>
                                    </div>
                                ) : (
                                    <div className="text-muted">{t('identity.audit_logs.diff_empty')}</div>
                                )}
                            </div>

                            <div>
                                <div className="fw-semibold">{t('identity.audit_logs.browser_info')}</div>
                                <pre className="p-2 bg-light border rounded mb-0" style={{ whiteSpace: 'pre-wrap' }}>
                                    {selectedLog.browserInfo || '-'}
                                </pre>
                            </div>

                            <div>
                                <div className="fw-semibold">{t('identity.audit_logs.exception')}</div>
                                <pre className="p-2 bg-light border rounded mb-0 text-danger" style={{ whiteSpace: 'pre-wrap' }}>
                                    {selectedLog.exception || '-'}
                                </pre>
                            </div>
                        </div>
                    )}
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setSelectedLog(null)}>
                        {t('common.close')}
                    </Button>
                </Modal.Footer>
            </Modal>
        </VerticalLayout>
    )
}

export default AuditLogs
