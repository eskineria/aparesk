namespace Aparesk.Eskineria.Core.Auth.Models;

public class PermissionInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
}

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class PermissionListDto
{
    public List<string> Permissions { get; set; } = new();
}

public class UserPermissionsDto
{
    public List<string> Direct { get; set; } = new();
    public List<string> Effective { get; set; } = new();
}

public class UserListDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ActiveRole { get; set; }
    public string? ProfilePicture { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class RoleListDto
{
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public List<string> Permissions { get; set; } = new();
    public List<string> UserAvatars { get; set; } = new();
}

public class PermissionDto
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public List<RoleListDto> AssignedRoles { get; set; } = new();
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateUserRolesRequest
{
    public Guid UserId { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UpdateRolePermissionsRequest
{
    public string RoleName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}

public class UpdateUserStatusRequest
{
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
}

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
}
