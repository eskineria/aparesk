import { useEffect, useState, useMemo } from 'react'
import { Card, CardFooter, CardHeader, Col, Container, Row, Badge } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import {
    createColumnHelper,
    getCoreRowModel,
    getFilteredRowModel,
    getPaginationRowModel,
    getSortedRowModel,
    useReactTable,
    type SortingState,
} from '@tanstack/react-table'
import { LuSearch } from 'react-icons/lu'

import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import DataTable from '@/components/table/DataTable'
import TablePagination from '@/components/table/TablePagination'
import { AccessControlService } from '@/services/accessControlService'
import type { PermissionDto } from '@/types/accessControl'
import { showToast } from '@/utils/toast'


const PermissionManagement = () => {
    const { t } = useTranslation()
    const [data, setData] = useState<PermissionDto[]>([])

    // Table states
    const [globalFilter, setGlobalFilter] = useState('')
    const [sorting, setSorting] = useState<SortingState>([])
    const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 10 })
    const [selectedRowIds, setSelectedRowIds] = useState<Record<string, boolean>>({})
    const [totalCount, setTotalCount] = useState(0)

    const fetchData = async () => {
        try {
            const permsResult = await AccessControlService.getPermissions({
                pageNumber: pagination.pageIndex + 1,
                pageSize: pagination.pageSize,
                searchTerm: globalFilter
            })

            setTotalCount(permsResult.totalCount ?? 0)
            setData(permsResult.items ?? [])
        } catch (error) {
            console.error(error)
            setTotalCount(0)
            setData([])
            showToast(t('identity.load_permissions_error'), 'error')
        }
    }

    // Debounce effect for search
    useEffect(() => {
        const timer = setTimeout(() => {
            fetchData()
        }, 500)

        return () => clearTimeout(timer)
    }, [pagination.pageIndex, pagination.pageSize, globalFilter])

    const columnHelper = createColumnHelper<PermissionDto>()

    const columns = useMemo(() => [
        columnHelper.accessor('name', {
            header: t('identity.permission_name'),
            cell: (info) => (
                <div>
                    <span className="fw-semibold text-dark">{info.getValue()}</span>
                    <div className="small text-muted">{info.row.original.group}</div>
                </div>
            )
        }),
        columnHelper.accessor('assignedRoles', {
            header: t('identity.assigned_to'),
            cell: ({ getValue }) => {
                const roles = getValue() ?? []
                return (
                    <div className="d-flex gap-1 flex-wrap">
                        {roles.map((role) => (
                            <Badge
                                key={role.name}
                                bg={role.name === 'Admin' ? 'danger-subtle' : 'info-subtle'}
                                className={`text-${role.name === 'Admin' ? 'danger' : 'info'} text-uppercase`}
                            >
                                {role.name}
                            </Badge>
                        ))}
                        {roles.length === 0 && <span className="text-muted small italic">{t('identity.not_assigned')}</span>}
                    </div>
                )
            },
            enableSorting: false,
        })
    ], [t])

    const table = useReactTable({
        data,
        columns,
        state: { sorting, globalFilter, pagination, rowSelection: selectedRowIds },
        onSortingChange: setSorting,
        onGlobalFilterChange: setGlobalFilter,
        onPaginationChange: setPagination,
        onRowSelectionChange: setSelectedRowIds,
        manualPagination: true,
        manualFiltering: true,
        autoResetPageIndex: false,
        pageCount: Math.ceil(totalCount / pagination.pageSize),
        getCoreRowModel: getCoreRowModel(),
        getSortedRowModel: getSortedRowModel(),
        getFilteredRowModel: getFilteredRowModel(),
        getPaginationRowModel: getPaginationRowModel(),
        // globalFilterFn: 'includesString', // Not needed for manual filtering
        enableRowSelection: false, // Permission definitions are usually immutable from UI in this design
    })

    // Calculate display range manually since we're server-side
    // const pageIndex = table.getState().pagination.pageIndex
    // const pageSize = table.getState().pagination.pageSize
    const start = pagination.pageIndex * pagination.pageSize + 1
    const end = Math.min(start + pagination.pageSize - 1, totalCount)

    return (
        <VerticalLayout>
            <Container fluid>
                <PageBreadcrumb title={t('identity.permissions')} subtitle={t('identity.title')} />

                <Row className="justify-content-center">
                    <Col xxl={12}>
                        <Card>
                            <CardHeader className="border-light justify-content-between">
                                <div className="d-flex gap-2">
                                    <div className="position-relative">
                                        <input
                                            type="search"
                                            className="form-control ps-5"
                                            placeholder={t('common.search') + "..."}
                                            value={globalFilter ?? ''}
                                            onChange={(e) => {
                                                setGlobalFilter(e.target.value)
                                                setPagination(prev => ({ ...prev, pageIndex: 0 }))
                                            }}
                                        />
                                        <LuSearch className="position-absolute top-50 start-0 translate-middle-y ms-3 text-muted" />
                                    </div>
                                </div>
                                <div className="d-flex align-items-center gap-2">
                                    <span className="text-muted small d-none d-md-inline">{t('common.show')}</span>
                                    <select
                                        value={table.getState().pagination.pageSize}
                                        onChange={(e) => table.setPageSize(Number(e.target.value))}
                                        className="form-select form-select-sm"
                                    >
                                        {[10, 20, 50, 100].map((size) => (
                                            <option key={size} value={size}>
                                                {size}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                            </CardHeader>
                            <DataTable<PermissionDto> table={table} emptyMessage={t('identity.no_permissions_found')} />
                            {totalCount > 0 && (
                                <CardFooter className="border-0">
                                    <TablePagination
                                        totalItems={totalCount}
                                        start={start}
                                        end={end}
                                        itemsName={t('identity.permissions').toLowerCase()}
                                        showInfo
                                        previousPage={table.previousPage}
                                        canPreviousPage={table.getCanPreviousPage()}
                                        pageCount={table.getPageCount()}
                                        pageIndex={table.getState().pagination.pageIndex}
                                        setPageIndex={table.setPageIndex}
                                        nextPage={table.nextPage}
                                        canNextPage={table.getCanNextPage()}
                                    />
                                </CardFooter>
                            )}
                        </Card>
                    </Col>
                </Row>
            </Container>
        </VerticalLayout>
    )
}

export default PermissionManagement
