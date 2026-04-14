import { useCallback, useEffect, useMemo, useState } from 'react'
import { Card, CardFooter, CardHeader, Container, Form, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import {
    type ColumnDef,
    createColumnHelper,
    getCoreRowModel,
    getSortedRowModel,
    type SortingState,
    useReactTable,
} from '@tanstack/react-table'
import { LuSearch } from 'react-icons/lu'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import DataTable from '@/components/table/DataTable'
import TablePagination from '@/components/table/TablePagination'
import EmailDeliveryLogService from '@/services/emailDeliveryLogService'
import type { EmailDeliveryLogItem } from '@/types/emailDeliveryLog'

const columnHelper = createColumnHelper<EmailDeliveryLogItem>()

const formatDateTime = (value: string, locale: string) => {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) {
        return value
    }

    return date.toLocaleString(locale)
}

const EmailDeliveryLogs = () => {
    const { t, i18n } = useTranslation()

    const [logs, setLogs] = useState<EmailDeliveryLogItem[]>([])
    const [totalCount, setTotalCount] = useState(0)
    const [pageNumber, setPageNumber] = useState(1)
    const [pageSize, setPageSize] = useState(20)
    const [isLoading, setIsLoading] = useState(false)
    const [searchTerm, setSearchTerm] = useState('')
    const [templateKey, setTemplateKey] = useState('')
    const [status, setStatus] = useState('')


    const columns = useMemo<ColumnDef<EmailDeliveryLogItem, unknown>[]>(() => ([
        columnHelper.accessor('createdAt', {
            header: t('identity.email_delivery_logs.created_at'),
            cell: ({ row }) => (
                <div className="d-flex flex-column">
                    <span className="fw-semibold">
                        {formatDateTime(row.original.createdAt, i18n.resolvedLanguage || i18n.language || 'en-US')}
                    </span>
                    <span className="text-muted fs-xs">#{row.original.id}</span>
                </div>
            ),
        }),
        columnHelper.accessor('recipient', {
            header: t('identity.email_delivery_logs.recipient'),
            cell: ({ row }) => (
                <div className="d-flex flex-column">
                    <span className="fw-semibold">{row.original.recipient}</span>
                    <span className="text-muted fs-xs">{row.original.subject}</span>
                </div>
            ),
        }),
        {
            id: 'template',
            header: t('identity.email_delivery_logs.template'),
            cell: ({ row }) => (
                <div className="d-flex flex-column">
                    <span>{row.original.templateKey || '-'}</span>
                    <span className="text-muted fs-xs">{row.original.culture || '-'}</span>
                </div>
            ),
        },
        {
            id: 'provider',
            header: t('identity.email_delivery_logs.provider'),
            cell: ({ row }) => {
                const normalizedStatus = (row.original.status || '').trim().toLowerCase()
                const isSuccess = normalizedStatus === 'success' || normalizedStatus === 'sent' || normalizedStatus === 'delivered'
                const badgeClass = isSuccess
                    ? 'badge bg-success-subtle text-success'
                    : 'badge bg-danger-subtle text-danger'

                return (
                    <div className="d-flex flex-column gap-1">
                        <span>{row.original.providerName || '-'}</span>
                        <span className={badgeClass}>
                            {row.original.status || '-'}
                        </span>
                    </div>
                )
            },
        },
        {
            id: 'error',
            header: t('identity.email_delivery_logs.error'),
            cell: ({ row }) => (
                <span className="text-muted">
                    {row.original.errorMessage || '-'}
                </span>
            ),
        },
    ]), [i18n.language, i18n.resolvedLanguage, t])

    const [sorting, setSorting] = useState<SortingState>([])

    const table = useReactTable({
        data: logs,
        columns,
        state: { sorting },
        onSortingChange: setSorting,
        getCoreRowModel: getCoreRowModel(),
        getSortedRowModel: getSortedRowModel(),
    })

    const fetchData = useCallback(async () => {
        setIsLoading(true)
        try {
            const response = await EmailDeliveryLogService.getPaged({
                pageNumber,
                pageSize,
                searchTerm: searchTerm.trim() || undefined,
                templateKey: templateKey.trim() || undefined,
                status: status || undefined,
            })
            // @ts-expect-error matching backend format
            setLogs(response.data ?? [])
            // @ts-expect-error matching backend format
            setTotalCount(response.count ?? 0)
        } catch {
            // Axios interceptor handles toast.
        } finally {
            setIsLoading(false)
        }
    }, [pageNumber, pageSize, searchTerm, status, templateKey])

    useEffect(() => {
        const timer = window.setTimeout(() => {
            void fetchData()
        }, 300)
        return () => window.clearTimeout(timer)
    }, [fetchData])

    const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1
    const endItem = Math.min(pageNumber * pageSize, totalCount)

    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('identity.email_delivery_logs.title')} subtitle={t('identity.title')} />
            <Container fluid>

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
                                    placeholder={t('identity.email_delivery_logs.search_placeholder')}
                                />
                                <LuSearch className="app-search-icon text-muted" />
                            </div>

                            <Form.Control
                                value={templateKey}
                                onChange={(event) => {
                                    setTemplateKey(event.target.value)
                                    setPageNumber(1)
                                }}
                                placeholder={t('identity.email_delivery_logs.template_key_placeholder')}
                                style={{ minWidth: 220 }}
                            />

                            <Form.Select
                                value={status}
                                onChange={(event) => {
                                    setStatus(event.target.value)
                                    setPageNumber(1)
                                }}
                                className="w-auto">
                                <option value="">{t('identity.email_delivery_logs.all_statuses')}</option>
                                <option value="Success">{t('identity.email_delivery_logs.status_success')}</option>
                                <option value="Failed">{t('identity.email_delivery_logs.status_failed')}</option>
                            </Form.Select>
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
                        </div>
                    </CardHeader>

                    {isLoading ? (
                        <div className="p-4 d-flex align-items-center justify-content-center gap-2 text-muted">
                            <Spinner animation="border" size="sm" />
                            <span>{t('common.loading')}</span>
                        </div>
                    ) : (
                        <DataTable<EmailDeliveryLogItem>
                            table={table}
                            emptyMessage={t('identity.email_delivery_logs.no_logs')}
                        />
                    )}

                    {totalCount > 0 && (
                        <CardFooter className="border-0">
                            <TablePagination
                                totalItems={totalCount}
                                start={startItem}
                                end={endItem}
                                itemsName={t('identity.email_delivery_logs.entries').toLowerCase()}
                                showInfo={true}
                                previousPage={() => setPageNumber((current) => Math.max(1, current - 1))}
                                canPreviousPage={pageNumber > 1}
                                pageCount={totalPages}
                                pageIndex={pageNumber - 1}
                                setPageIndex={(index) => setPageNumber(index + 1)}
                                nextPage={() => setPageNumber((current) => Math.min(totalPages, current + 1))}
                                canNextPage={pageNumber < totalPages}
                            />
                        </CardFooter>
                    )}
                </Card>
            </Container>
        </VerticalLayout>
    )
}

export default EmailDeliveryLogs
