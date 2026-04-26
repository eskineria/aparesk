using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Auth.Authorization;
using Aparesk.Eskineria.Core.Auth.Constants;
using Aparesk.Eskineria.Core.Auth.Models;
using Aparesk.Eskineria.Core.Shared.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Aparesk.Eskineria.Core.Auth.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[HasPermission("Dashboard", "View")]
public sealed class AccessControlController : AuthApiControllerBase
{
    private readonly IAccessControlService _accessControlService;
    private readonly IStringLocalizer<AccessControlController> _localizer;

    public AccessControlController(
        IAccessControlService accessControlService,
        IStringLocalizer<AccessControlController> localizer)
    {
        _accessControlService = accessControlService;
        _localizer = localizer;
    }

    [HttpGet("users")]
    [HasPermission("Users", "Read")]
    public async Task<IActionResult> GetUsers([FromQuery] PagedRequest request)
    {
        var response = await _accessControlService.GetUsersAsync(request);
        return FromResponse(response);
    }

    [HttpGet("roles")]
    [HasPermission("Roles", "Read")]
    [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "searchTerm" }, VaryByHeader = "Authorization")]
    public async Task<IActionResult> GetRoles([FromQuery] PagedRequest request)
    {
        var response = await _accessControlService.GetRolesAsync(request);
        return FromResponse(response);
    }

    [HttpGet("permissions")]
    [HasPermission("Roles", "Read")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "searchTerm" }, VaryByHeader = "Authorization")]
    public async Task<IActionResult> GetAllPermissions([FromQuery] PagedRequest request)
    {
        var response = await _accessControlService.GetAllPermissionsAsync(request);
        return FromResponse(response);
    }

    [HttpPost("roles")]
    [HasPermission("Roles", "Manage")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var response = await _accessControlService.CreateRoleAsync(request.Name);
        return FromResponse(response);
    }

    [HttpDelete("roles/{name}")]
    [HasPermission("Roles", "Manage")]
    public async Task<IActionResult> DeleteRole([FromRoute] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(Aparesk.Eskineria.Core.Shared.Response.Response.Fail(_localizer[AuthLocalizationKeys.RoleNameRequired]));
        }

        var response = await _accessControlService.DeleteRoleAsync(name);
        return FromResponse(response);
    }

    [HttpPost("update-user-roles")]
    [HasPermission("Users", "Manage")]
    public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesRequest request)
    {
        var response = await _accessControlService.UpdateUserRolesAsync(request);
        return FromResponse(response);
    }

    [HttpPost("update-role-permissions")]
    [HasPermission("Roles", "Manage")]
    public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsRequest request)
    {
        var response = await _accessControlService.UpdateRolePermissionsAsync(request);
        return FromResponse(response);
    }

    [HttpPost("update-user-status")]
    [HasPermission("Users", "Manage")]
    public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusRequest request)
    {
        var response = await _accessControlService.UpdateUserStatusAsync(request);
        return FromResponse(response);
    }
}
