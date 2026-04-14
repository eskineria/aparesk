using Eskineria.Core.Auth.Models;
using Eskineria.Core.Shared.Response;

namespace Eskineria.Core.Auth.Abstractions;

public interface IAccessControlService
{
    Task<PagedResponse<UserListDto>> GetUsersAsync(PagedRequest request);
    Task<PagedResponse<RoleListDto>> GetRolesAsync(PagedRequest request);
    Task<PagedResponse<PermissionDto>> GetAllPermissionsAsync(PagedRequest request);

    Task<Response> CreateRoleAsync(string roleName);
    Task<Response> DeleteRoleAsync(string roleName);

    Task<Response> UpdateUserRolesAsync(UpdateUserRolesRequest request);
    Task<Response> UpdateRolePermissionsAsync(UpdateRolePermissionsRequest request);
    Task<Response> UpdateUserStatusAsync(UpdateUserStatusRequest request);
}
