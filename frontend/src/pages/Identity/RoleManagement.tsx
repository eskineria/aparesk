import { useEffect, useState } from 'react'
import { Button, Modal, Form, Container, Row, Col } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import { AccessControlService } from '@/services/accessControlService'
import type { RoleListDto, PermissionDto } from '@/types/accessControl'
import { showToast } from '@/utils/toast'
import { LuPlus } from 'react-icons/lu'
import MemberRoleCard from './components/MemberRoleCard'
import TablePagination from '@/components/table/TablePagination'
import ConfirmationModal from '@/components/ConfirmationModal'

const RoleManagement = () => {
    const { t } = useTranslation()
    const [roles, setRoles] = useState<RoleListDto[]>([])
    const [allPermissions, setAllPermissions] = useState<PermissionDto[]>([])

    // Pagination
    const [pageNumber, setPageNumber] = useState(1)
    const pageSize = 12
    const [totalCount, setTotalCount] = useState(0)

    // Modal states
    const [showRoleModal, setShowRoleModal] = useState(false)
    const [showPermissionModal, setShowPermissionModal] = useState(false)
    const [newRoleName, setNewRoleName] = useState('')
    const [selectedRole, setSelectedRole] = useState<RoleListDto | null>(null)
    const [rolePermissions, setRolePermissions] = useState<string[]>([])
    const [deleteRoleTarget, setDeleteRoleTarget] = useState<string | null>(null)
    const [isDeletingRole, setIsDeletingRole] = useState(false)

    const fetchData = async () => {
        try {
            const [rolesData, permsData] = await Promise.all([
                AccessControlService.getRoles({ pageNumber, pageSize }),
                AccessControlService.getPermissions()
            ])
            setRoles(rolesData.items ?? [])
            setTotalCount(rolesData.totalCount ?? 0)
            setAllPermissions(permsData.items || [])
        } catch (error) {
            console.error(error)
            setRoles([])
            setAllPermissions([])
            setTotalCount(0)
        }
    }

    useEffect(() => {
        fetchData()
    }, [pageNumber, pageSize])

    const handleCreateRole = async () => {
        if (!newRoleName.trim()) return
        try {
            await AccessControlService.createRole({ name: newRoleName })
            showToast(t('identity.role_created'), 'success')
            setNewRoleName('')
            setShowRoleModal(false)
            fetchData()
        } catch (error) {
            console.error(error)
        }
    }

    const handleDeleteRole = (roleName: string) => {
        setDeleteRoleTarget(roleName)
    }

    const confirmDeleteRole = async () => {
        if (!deleteRoleTarget) {
            return
        }

        setIsDeletingRole(true)
        try {
            await AccessControlService.deleteRole(deleteRoleTarget)
            showToast(t('identity.role_deleted'), 'success')
            fetchData()
        } catch (error) {
            console.error(error)
        } finally {
            setDeleteRoleTarget(null)
            setIsDeletingRole(false)
        }
    }

    const handleOpenPermissionModal = (role: RoleListDto) => {
        setSelectedRole(role)
        setRolePermissions(role.permissions ?? [])
        setShowPermissionModal(true)
    }

    const handleSavePermissions = async () => {
        if (!selectedRole) return
        try {
            await AccessControlService.updateRolePermissions({
                roleName: selectedRole.name,
                permissions: rolePermissions
            })
            showToast(t('identity.permissions_updated'), 'success')
            setShowPermissionModal(false)
            fetchData()
        } catch (error) {
            console.error(error)
        }
    }

    // Group permissions by category
    const groupedPermissions = Array.isArray(allPermissions)
        ? allPermissions.reduce((acc, curr) => {
            if (!acc[curr.group]) acc[curr.group] = []
            acc[curr.group].push(curr)
            return acc
        }, {} as Record<string, PermissionDto[]>)
        : {}

    const totalPages = Math.ceil(totalCount / pageSize)
    const startItem = (pageNumber - 1) * pageSize + 1
    const endItem = Math.min(pageNumber * pageSize, totalCount)

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('identity.roles')} subtitle={t('identity.title')} />
            <Container fluid>
                <div className="d-flex align-items-sm-center flex-sm-row flex-column my-3">
                    <div className="flex-grow-1">
                        <h4 className="fs-4 mb-1">{t('identity.roles')}</h4>
                        <p className="text-muted mb-0">{t('identity.manage_roles_description')}</p>
                    </div>
                    <div className="text-end mt-2 mt-sm-0">
                        <Button variant="success" onClick={() => setShowRoleModal(true)}>
                            <LuPlus className="me-1" /> {t('identity.create_role')}
                        </Button>
                    </div>
                </div>

                <Row>
                    {roles.map((role) => (
                        <Col md={6} xl={4} key={role.name} className="mb-3">
                            <MemberRoleCard
                                role={role}
                                onEdit={handleOpenPermissionModal}
                                onDelete={handleDeleteRole}
                            />
                        </Col>
                    ))}
                    {roles.length === 0 && (
                        <Col xs={12}>
                            <div className="text-center p-5 text-muted">{t('identity.no_roles_found')}</div>
                        </Col>
                    )}
                </Row>

                {/* Pagination */}
                {totalCount > 0 && (
                    <TablePagination
                        totalItems={totalCount}
                        start={startItem}
                        end={endItem}
                        itemsName={t('identity.roles').toLowerCase()}
                        showInfo={true}
                        previousPage={() => setPageNumber(p => Math.max(1, p - 1))}
                        canPreviousPage={pageNumber > 1}
                        pageCount={totalPages}
                        pageIndex={pageNumber - 1}
                        setPageIndex={(idx) => setPageNumber(idx + 1)}
                        nextPage={() => setPageNumber(p => Math.min(totalPages, p + 1))}
                        canNextPage={pageNumber < totalPages}
                        className="pagination-rounded justify-content-center"
                    />
                )}

                {/* Create Role Modal */}
                <Modal show={showRoleModal} onHide={() => setShowRoleModal(false)}>
                    <Modal.Header closeButton>
                        <Modal.Title>{t('identity.create_role')}</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <Form.Group>
                            <Form.Label>{t('identity.role_name')}</Form.Label>
                            <Form.Control
                                type="text"
                                placeholder={t('identity.role_placeholder')}
                                value={newRoleName}
                                onChange={(e) => setNewRoleName(e.target.value)}
                            />
                        </Form.Group>
                    </Modal.Body>
                    <Modal.Footer>
                        <Button variant="light" onClick={() => setShowRoleModal(false)}>{t('common.cancel')}</Button>
                        <Button variant="primary" onClick={handleCreateRole}>{t('common.save')}</Button>
                    </Modal.Footer>
                </Modal>

                {/* Permissions Modal */}
                <Modal show={showPermissionModal} onHide={() => setShowPermissionModal(false)} size="lg">
                    {/* ... (Permission modal content unchanged) ... */}
                    <Modal.Header closeButton>
                        <Modal.Title>{t('identity.manage_role_permissions')}: {selectedRole?.name}</Modal.Title>
                    </Modal.Header>
                    <Modal.Body className="p-4" style={{ maxHeight: '70vh', overflowY: 'auto' }}>
                        {Object.keys(groupedPermissions).map(group => {
                            const groupPerms = groupedPermissions[group]
                            const allSelected = groupPerms.every(p => rolePermissions.includes(p.name))
                            const someSelected = groupPerms.some(p => rolePermissions.includes(p.name))

                            return (
                                <div key={group} className="mb-4">
                                    <div className="d-flex align-items-center justify-content-between border-bottom pb-2 mb-3">
                                        <h6 className="text-uppercase text-muted fw-bold m-0">
                                            {t(`identity.groups.${group}`, { defaultValue: group })}
                                        </h6>
                                        <Form.Check
                                            type="checkbox"
                                            id={`select-all-${group}`}
                                            label={t('common.select_all')}
                                            checked={allSelected}
                                            ref={(input) => {
                                                if (input) {
                                                    input.indeterminate = !allSelected && someSelected
                                                }
                                            }}
                                            onChange={(e) => {
                                                const permNames = groupPerms.map(p => p.name)
                                                if (e.target.checked) {
                                                    // Add all missing permissions from this group
                                                    const toAdd = permNames.filter(name => !rolePermissions.includes(name))
                                                    setRolePermissions([...rolePermissions, ...toAdd])
                                                } else {
                                                    // Remove all permissions from this group
                                                    setRolePermissions(rolePermissions.filter(name => !permNames.includes(name)))
                                                }
                                            }}
                                        />
                                    </div>
                                    <Row>
                                        {groupPerms.map(perm => (
                                            <Col md={6} key={perm.name} className="mb-2">
                                                <Form.Check
                                                    type="checkbox"
                                                    id={`perm-${perm.name}`}
                                                    label={t(`identity.names.${perm.name}`, { defaultValue: perm.name })}
                                                    checked={rolePermissions.includes(perm.name)}
                                                    onChange={(e) => {
                                                        if (e.target.checked) {
                                                            setRolePermissions([...rolePermissions, perm.name])
                                                        } else {
                                                            setRolePermissions(rolePermissions.filter(p => p !== perm.name))
                                                        }
                                                    }}
                                                />
                                            </Col>
                                        ))}
                                    </Row>
                                </div>
                            )
                        })}
                    </Modal.Body>
                    <Modal.Footer>
                        <Button variant="light" onClick={() => setShowPermissionModal(false)}>{t('common.cancel')}</Button>
                        <Button variant="primary" onClick={handleSavePermissions}>{t('common.save')}</Button>
                    </Modal.Footer>
                </Modal>

                <ConfirmationModal
                    show={deleteRoleTarget != null}
                    onHide={() => setDeleteRoleTarget(null)}
                    onConfirm={() => void confirmDeleteRole()}
                    title={t('common.confirmAction')}
                    message={t('common.confirmProceed')}
                    variant="danger"
                    confirmText={t('common.delete')}
                    isLoading={isDeletingRole}
                />
            </Container>
        </VerticalLayout>
    )
}

export default RoleManagement
