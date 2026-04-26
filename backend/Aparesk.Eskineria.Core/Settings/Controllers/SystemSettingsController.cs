using Aparesk.Eskineria.Core.Settings.Abstractions;
using Aparesk.Eskineria.Core.Settings.Models;
using Aparesk.Eskineria.Core.Auth.Authorization;
using Aparesk.Eskineria.Core.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aparesk.Eskineria.Core.Settings.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SystemSettingsController : ApiControllerBase
{
    private readonly ISystemSettingsService _systemSettingsService;

    public SystemSettingsController(ISystemSettingsService systemSettingsService)
    {
        _systemSettingsService = systemSettingsService;
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicAuthSettings()
    {
        var response = await _systemSettingsService.GetAuthSettingsAsync();

        if (response.Success && response.Data != null)
        {
            response.Data.MfaBypassIpWhitelist = string.Empty;
            response.Data.RegistrationAllowedEmailDomains = string.Empty;
            response.Data.RegistrationBlockedEmailDomains = string.Empty;
            response.Data.MaintenanceIpWhitelist = string.Empty;
            response.Data.MaintenanceRoleWhitelist = string.Empty;
            response.Data.EmailSenderName = string.Empty;
            response.Data.EmailSenderAddress = string.Empty;
            response.Data.NotificationSecurityEmailRecipients = string.Empty;
        }

        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Settings", "Read")]
    [HttpGet]
    public async Task<IActionResult> GetAuthSettings()
    {
        var response = await _systemSettingsService.GetAuthSettingsAsync();
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Settings", "Manage")]
    [HttpPut]
    public async Task<IActionResult> UpdateAuthSettings([FromBody] UpdateAuthSystemSettingsRequest request)
    {
        var response = await _systemSettingsService.UpdateAuthSettingsAsync(request);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Settings", "Manage")]
    [HttpPost("branding/logo")]
    public async Task<IActionResult> UploadApplicationLogo([FromForm] IFormFile file)
    {
        var response = await _systemSettingsService.UploadApplicationLogoAsync(file);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Settings", "Manage")]
    [HttpPost("branding/favicon")]
    public async Task<IActionResult> UploadApplicationFavicon([FromForm] IFormFile file)
    {
        var response = await _systemSettingsService.UploadApplicationFaviconAsync(file);
        return FromResponse(response);
    }
}
