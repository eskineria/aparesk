import { useEffect, useState } from 'react'
import { Button, Modal, Form, Container, Card, CardHeader, CardFooter } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import { AccessControlService } from '@/services/accessControlService'
import type { UserListDto, RoleListDto } from '@/types/accessControl'
import { showToast } from '@/utils/toast'
import DataTable from '@/components/table/DataTable'
import TablePagination from '@/components/table/TablePagination'
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
import { LuSearch, LuUserCheck, LuUserCog, LuUserX } from 'react-icons/lu'

const columnHelper = createColumnHelper<UserListDto>()

const UserManagement = () => {
    const { t } = useTranslation()
    const [users, setUsers] = useState<UserListDto[]>([])
    const [roles, setRoles] = useState<RoleListDto[]>([])

    // Pagination states
    const [pageNumber, setPageNumber] = useState(1)
    const [pageSize, setPageSize] = useState(10)
    const [totalCount, setTotalCount] = useState(0)

    // Search state
    const [searchTerm, setSearchTerm] = useState('')

    // Modal states
    const [showRoleModal, setShowRoleModal] = useState(false)
    const [selectedUser, setSelectedUser] = useState<UserListDto | null>(null)
    const [userRoles, setUserRoles] = useState<string[]>([])
    const [isSavingRoles, setIsSavingRoles] = useState(false)

    const columns: ColumnDef<UserListDto, any>[] = [
        columnHelper.accessor('firstName', {
            header: t('identity.table.user'),
            cell: ({ row }) => (
                <div className="d-flex align-items-center gap-2">
                    {row.original.profilePicture ? (
                        <div className="avatar avatar-sm">
                            <img src={row.original.profilePicture} className="img-fluid rounded-circle" alt="" width={32} height={32} />
                        </div>
                    ) : (
                        <div className="avatar avatar-sm bg-primary-subtle text-primary rounded-circle d-flex align-items-center justify-content-center fw-bold">
                            {row.original.firstName?.[0]}{row.original.lastName?.[0]}
                        </div>
                    )}
                    <div>
                        <h5 className="fs-base mb-0">
                            <a href="#" className="link-reset">
                                {row.original.firstName} {row.original.lastName}
                            </a>
                        </h5>
                        <p className="text-muted fs-xs mb-0">{row.original.email}</p>
                    </div>
                </div>
            ),
        }),
        columnHelper.accessor('activeRole', {
            header: t('identity.table.active_role'),
            cell: ({ row }) => row.original.activeRole || <span className="text-muted">-</span>
        }),
        columnHelper.accessor('roles', {
            header: t('identity.table.assigned_roles'),
            cell: ({ row }) => (
                <div className="d-flex flex-wrap gap-1">
                    {row.original.roles && row.original.roles.length > 0 ? (
                        row.original.roles.map((role) => (
                            <span key={role} className={`badge ${role === 'Admin' ? 'bg-danger-subtle text-danger' : 'bg-info-subtle text-info'}`}>
                                {role}
                            </span>
                        ))
                    ) : (
                        <span className="text-muted">-</span>
                    )}
                </div>
            ),
        }),
        columnHelper.accessor('isActive', {
            header: t('identity.table.status'),
            cell: ({ row }) => (
                <span className={`badge ${row.original.isActive ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger'} badge-label`}>
                    {row.original.isActive ? t('identity.status_active') : t('identity.status_inactive')}
                </span>
            ),
        }),
        {
            header: t('identity.table.actions'),
            cell: ({ row }: { row: TableRow<UserListDto> }) => (
                <div className="d-flex gap-1">
                    <Button
                        variant="default"
                        size="sm"
                        className="btn-icon"
                        onClick={() => handleOpenRoleModal(row.original)}
                        title={t('identity.table.manage_roles')}
                    >
                        <LuUserCog className="fs-lg" />
                    </Button>
                    <Button
                        variant="default"
                        size="sm"
                        className="btn-icon"
                        onClick={() => handleToggleStatus(row.original)}
                        title={t('identity.table.toggle_status')}
                    >
                        {row.original.isActive ? <LuUserX className="fs-lg" /> : <LuUserCheck className="fs-lg" />}
                    </Button>
                </div>
            ),
        },
    ]

    const [sorting, setSorting] = useState<SortingState>([])
    const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 10 })

    const table = useReactTable({
        data: users,
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
        try {
            const [usersData, rolesData] = await Promise.all([
                AccessControlService.getUsers({
                    pageNumber,
                    pageSize,
                    searchTerm
                }),
                AccessControlService.getRoles({ pageNumber: 1, pageSize: 100 })
            ])
            setUsers(usersData.items ?? [])
            setTotalCount(usersData.totalCount ?? 0)
            setRoles(rolesData.items ?? [])

        } catch (error) {
            console.error(error)
            setUsers([])
            setRoles([])
            setTotalCount(0)
        }
    }

    // Debounce effect
    useEffect(() => {
        const timer = setTimeout(() => {
            fetchData()
        }, 500)

        return () => clearTimeout(timer)
    }, [pageNumber, pageSize, searchTerm])

    const handleToggleStatus = async (user: UserListDto) => {
        try {
            await AccessControlService.updateUserStatus({
                userId: user.id,
                isActive: !user.isActive
            })
            showToast(t('common.success'), 'success')
            fetchData()
        } catch (error) {
            console.error(error)
        }
    }

    const handleOpenRoleModal = (user: UserListDto) => {
        setSelectedUser(user)
        setUserRoles([...(user.roles || [])])
        setShowRoleModal(true)
    }

    const handleCloseRoleModal = (force = false) => {
        if (isSavingRoles && !force) return
        setShowRoleModal(false)
        setSelectedUser(null)
        setUserRoles([])
    }

    const handleSaveRoles = async () => {
        if (!selectedUser || isSavingRoles || userRoles.length === 0) return
        setIsSavingRoles(true)
        try {
            await AccessControlService.updateUserRoles({
                userId: selectedUser.id,
                roles: userRoles
            })
            showToast(t('identity.roles_updated'), 'success')
            handleCloseRoleModal(true)
            await fetchData()
        } catch (error) {
            console.error(error)
        } finally {
            setIsSavingRoles(false)
        }
    }

    // Pagination Helpers
    const startItem = (pageNumber - 1) * pageSize + 1
    const endItem = Math.min(pageNumber * pageSize, totalCount)

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('identity.users')} subtitle={t('identity.title')} />
            <Container fluid>
                <Card>
                    <CardHeader className="border-light justify-content-between">
                        <div className="d-flex gap-2">
                            <div className="app-search">
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
                        </div>
                        <div className="d-flex align-items-center gap-2">
                            <span className="me-2 fw-semibold">{t('identity.table.show')}:</span>
                            <div>
                                <select
                                    value={pageSize}
                                    onChange={(e) => {
                                        setPageSize(Number(e.target.value))
                                        setPageNumber(1)
                                    }}
                                    className="form-select form-control my-1 my-md-0"
                                >
                                    {[5, 10, 15, 20, 50].map((size) => (
                                        <option key={size} value={size}>
                                            {size}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>
                    </CardHeader>
                    <DataTable<UserListDto> table={table} emptyMessage={t('identity.no_users_found')} />
                    {users.length > 0 && (
                        <CardFooter className="border-0">
                            <TablePagination
                                totalItems={totalCount}
                                start={startItem}
                                end={endItem}
                                itemsName={t('identity.users').toLowerCase()}
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
                    )}
                </Card>

                {/* Roles Modal */}
                <Modal show={showRoleModal} onHide={handleCloseRoleModal}>
                    <Modal.Header closeButton>
                        <Modal.Title>{t('identity.manage_user_roles')}: {selectedUser?.firstName} {selectedUser?.lastName}</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <Form>
                            {roles.map((role) => (
                                <Form.Check
                                    key={role.name}
                                    type="checkbox"
                                    id={`role-${role.name}`}
                                    label={role.name}
                                    checked={userRoles.includes(role.name)}
                                    disabled={isSavingRoles}
                                    onChange={(e) => {
                                        setUserRoles((prevRoles) => {
                                            if (e.target.checked) {
                                                return prevRoles.includes(role.name)
                                                    ? prevRoles
                                                    : [...prevRoles, role.name]
                                            }

                                            return prevRoles.filter(r => r !== role.name)
                                        })
                                    }}
                                    className="mb-2"
                                />
                            ))}
                        </Form>
                    </Modal.Body>
                    <Modal.Footer>
                        <Button variant="light" onClick={() => handleCloseRoleModal()} disabled={isSavingRoles}>{t('common.cancel')}</Button>
                        <Button variant="primary" onClick={handleSaveRoles} disabled={isSavingRoles || userRoles.length === 0}>
                            {isSavingRoles ? t('common.loading') : t('common.save')}
                        </Button>
                    </Modal.Footer>
                </Modal>
            </Container>
        </VerticalLayout>
    )
}

export default UserManagement
