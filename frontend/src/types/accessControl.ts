export interface UserListDto {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    isActive: boolean;
    activeRole?: string | null;
    profilePicture?: string | null;
    roles: string[];
}

export interface RoleListDto {
    name: string;
    userCount: number;
    permissions: string[];
    userAvatars: string[];
}

export interface PermissionDto {
    name: string;
    group: string;
    assignedRoles: RoleListDto[];
}

export interface CreateRoleRequest {
    name: string;
}

export interface UpdateUserRolesRequest {
    userId: string;
    roles: string[];
}

export interface UpdateRolePermissionsRequest {
    roleName: string;
    permissions: string[];
}

export interface UpdateUserStatusRequest {
    userId: string;
    isActive: boolean;
}

export interface PagedRequest {
    pageNumber: number;
    pageSize: number;
    searchTerm?: string;
}

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}
