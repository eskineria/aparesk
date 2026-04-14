import api from '@/api/axios'
import type {
    UserListDto,
    RoleListDto,
    PermissionDto,
    CreateRoleRequest,
    UpdateUserRolesRequest,
    UpdateRolePermissionsRequest,
    UpdateUserStatusRequest,
    PagedRequest,
    PagedResult
} from '@/types/accessControl'

type BackendPagedResponse<T> = {
    data?: T[] | null
    count?: number | null
    index?: number | null
    size?: number | null
    pages?: number | null
}

const normalizePagedResult = <T>(payload?: BackendPagedResponse<T> | null): PagedResult<T> => {
    const items = Array.isArray(payload?.data) ? payload!.data : []
    const totalCount = typeof payload?.count === 'number' ? payload.count : 0
    const pageNumber = typeof payload?.index === 'number' ? payload.index + 1 : 1
    const pageSize = typeof payload?.size === 'number' ? payload.size : items.length
    const totalPages = typeof payload?.pages === 'number' ? payload.pages : 0

    return {
        items,
        totalCount,
        pageNumber,
        pageSize,
        totalPages,
    }
}

export const AccessControlService = {
    getUsers: async (params?: PagedRequest) => {
        const response = await api.get<BackendPagedResponse<UserListDto>>('/AccessControl/users', { params })
        return normalizePagedResult(response.data)
    },

    getRoles: async (params?: PagedRequest) => {
        const response = await api.get<BackendPagedResponse<RoleListDto>>('/AccessControl/roles', { params })
        return normalizePagedResult(response.data)
    },

    getPermissions: async (params?: PagedRequest) => {
        const response = await api.get<BackendPagedResponse<PermissionDto>>('/AccessControl/permissions', { params })
        return normalizePagedResult(response.data)
    },

    createRole: async (data: CreateRoleRequest) => {
        const response = await api.post('/AccessControl/roles', data)
        return response.data
    },

    deleteRole: async (roleName: string) => {
        const response = await api.delete(`/AccessControl/roles/${roleName}`)
        return response.data
    },

    updateUserRoles: async (data: UpdateUserRolesRequest) => {
        const response = await api.post('/AccessControl/update-user-roles', data)
        return response.data
    },

    updateRolePermissions: async (data: UpdateRolePermissionsRequest) => {
        const response = await api.post('/AccessControl/update-role-permissions', data)
        return response.data
    },

    updateUserStatus: async (data: UpdateUserStatusRequest) => {
        const response = await api.post('/AccessControl/update-user-status', data)
        return response.data
    }
}
