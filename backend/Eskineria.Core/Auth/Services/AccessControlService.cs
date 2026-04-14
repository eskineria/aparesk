using System.Security.Claims;
using Eskineria.Core.Auth.Abstractions;
using Eskineria.Core.Auth.Constants;
using Eskineria.Core.Auth.Entities;
using Eskineria.Core.Auth.Models;
using Eskineria.Core.Shared.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Eskineria.Core.Auth.Services;

public sealed class AccessControlService : IAccessControlService
{
    private readonly UserManager<EskineriaUser> _userManager;
    private readonly RoleManager<EskineriaRole> _roleManager;
    private readonly IPermissionDiscoveryService _discoveryService;
    private readonly IStringLocalizer<AccessControlService> _localizer;

    public AccessControlService(
        UserManager<EskineriaUser> userManager,
        RoleManager<EskineriaRole> roleManager,
        IPermissionDiscoveryService discoveryService,
        IStringLocalizer<AccessControlService> localizer)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _discoveryService = discoveryService;
        _localizer = localizer;
    }

    public async Task<PagedResponse<UserListDto>> GetUsersAsync(PagedRequest request)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var userDtos = new List<UserListDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserListDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                ActiveRole = user.ActiveRole,
                ProfilePicture = user.ProfilePicture,
                Roles = roles.ToList()
            });
        }

        return CreatePagedResponse(userDtos, request.PageNumber, request.PageSize, totalCount);
    }

    public async Task<PagedResponse<RoleListDto>> GetRolesAsync(PagedRequest request)
    {
        var query = _roleManager.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(r => r.Name != null && r.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();
        var roles = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var roleDtos = new List<RoleListDto>();
        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToList();

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            roleDtos.Add(new RoleListDto
            {
                Name = role.Name!,
                UserCount = usersInRole.Count,
                Permissions = permissions,
                UserAvatars = usersInRole
                    .Where(u => !string.IsNullOrEmpty(u.ProfilePicture))
                    .Select(u => u.ProfilePicture!)
                    .Take(5)
                    .ToList()
            });
        }

        return CreatePagedResponse(roleDtos, request.PageNumber, request.PageSize, totalCount);
    }

    public async Task<PagedResponse<PermissionDto>> GetAllPermissionsAsync(PagedRequest request)
    {
        var query = _discoveryService.GetAllPermissions().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(p => p.ToLower().Contains(term));
        }

        var totalCount = query.Count();
        var pagedPermissions = query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PermissionDto
            {
                Name = p,
                Group = p.Split('.')[0]
            })
            .ToList();

        var roles = await _roleManager.Roles.ToListAsync();
        var permissionsByRole = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            var roleClaims = await _roleManager.GetClaimsAsync(role);
            permissionsByRole[role.Name ?? string.Empty] = roleClaims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var permission in pagedPermissions)
        {
            var assignedRoles = permissionsByRole
                .Where(x => x.Value.Contains(permission.Name))
                .Select(x => new RoleListDto { Name = x.Key })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .OrderBy(x => x.Name)
                .ToList();

            if (!assignedRoles.Any(r => string.Equals(r.Name, Permissions.AdminRole, StringComparison.OrdinalIgnoreCase)))
            {
                assignedRoles.Add(new RoleListDto { Name = Permissions.AdminRole });
            }

            permission.AssignedRoles = assignedRoles;
        }

        return CreatePagedResponse(pagedPermissions, request.PageNumber, request.PageSize, totalCount);
    }

    public async Task<Response> CreateRoleAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return Response.Fail(_localizer[AuthLocalizationKeys.RoleAlreadyExists]);
        }

        var result = await _roleManager.CreateAsync(new EskineriaRole(roleName));
        return result.Succeeded
            ? Response.Succeed(_localizer[AuthLocalizationKeys.RoleCreated])
            : Response.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Response> DeleteRoleAsync(string roleName)
    {
        if (roleName == Permissions.AdminRole)
        {
            return Response.Fail(_localizer[AuthLocalizationKeys.CannotDeleteAdminRole]);
        }

        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return Response.Fail(_localizer[AuthLocalizationKeys.RoleNotFound]);
        }

        var result = await _roleManager.DeleteAsync(role);
        return result.Succeeded
            ? Response.Succeed(_localizer[AuthLocalizationKeys.RoleDeleted])
            : Response.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Response> UpdateUserRolesAsync(UpdateUserRolesRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return Response.Fail(_localizer[AuthLocalizationKeys.UserNotFound]);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return Response.Fail(_localizer[AuthLocalizationKeys.FailedToRemoveCurrentRoles]);
        }

        var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
        if (!addResult.Succeeded)
        {
            return Response.Fail(_localizer[AuthLocalizationKeys.FailedToAddNewRoles]);
        }

        return Response.Succeed(_localizer[AuthLocalizationKeys.UserRolesUpdated]);
    }

    public async Task<Response> UpdateRolePermissionsAsync(UpdateRolePermissionsRequest request)
    {
        var role = await _roleManager.FindByNameAsync(request.RoleName);
        if (role == null)
        {
            return Response.Fail(_localizer[AuthLocalizationKeys.RoleNotFound]);
        }

        var currentClaims = await _roleManager.GetClaimsAsync(role);
        var currentPermissions = currentClaims.Where(c => c.Type == Permissions.ClaimType);
        foreach (var claim in currentPermissions)
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var permission in request.Permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
        }

        return Response.Succeed(_localizer[AuthLocalizationKeys.PermissionsUpdated]);
    }

    public async Task<Response> UpdateUserStatusAsync(UpdateUserStatusRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return Response.Fail(_localizer[AuthLocalizationKeys.UserNotFound]);
        }

        user.IsActive = request.IsActive;
        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded
            ? Response.Succeed(_localizer[AuthLocalizationKeys.UserStatusUpdated])
            : Response.Fail(_localizer[AuthLocalizationKeys.FailedToUpdateUserStatus]);
    }

    private static PagedResponse<T> CreatePagedResponse<T>(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        var index = pageNumber - 1;
        var pages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var hasPrevious = index > 0;
        var hasNext = index + 1 < pages;

        return new PagedResponse<T>(
            items,
            index,
            pageSize,
            totalCount,
            pages,
            hasPrevious,
            hasNext);
    }
}
