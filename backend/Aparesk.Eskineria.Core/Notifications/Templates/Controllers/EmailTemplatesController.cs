using Aparesk.Eskineria.Core.Notifications.Templates.Abstractions;
using Aparesk.Eskineria.Core.Notifications.Templates.Models;
using Aparesk.Eskineria.Core.Auth.Authorization;
using Aparesk.Eskineria.Core.Shared.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aparesk.Eskineria.Core.Notifications.Templates.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class EmailTemplatesController : ApiControllerBase
{
    private readonly IEmailTemplateService _emailTemplateService;

    public EmailTemplatesController(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    [Authorize]
    [HasPermission("Email", "Read")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? culture = null)
    {
        var response = await _emailTemplateService.GetAllAsync(culture);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Read")]
    [HttpGet("coverage")]
    public async Task<IActionResult> GetCoverage()
    {
        var response = await _emailTemplateService.GetCoverageAsync();
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Read")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var response = await _emailTemplateService.GetByIdAsync(id);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Manage")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmailTemplateRequest request)
    {
        var response = await _emailTemplateService.CreateAsync(request);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Manage")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateEmailTemplateRequest request)
    {
        var response = await _emailTemplateService.UpdateAsync(id, request);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Manage")]
    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish([FromRoute] int id, [FromBody] PublishEmailTemplateRequest request)
    {
        var response = await _emailTemplateService.PublishAsync(id, request);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Manage")]
    [HttpPost("{id:int}/rollback")]
    public async Task<IActionResult> Rollback([FromRoute] int id, [FromBody] RollbackEmailTemplateRequest request)
    {
        var response = await _emailTemplateService.RollbackAsync(id, request);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Read")]
    [HttpGet("{id:int}/versions")]
    public async Task<IActionResult> GetVersions([FromRoute] int id)
    {
        var response = await _emailTemplateService.GetVersionsAsync(id);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Manage")]
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateEmailTemplateRequest request)
    {
        var response = await _emailTemplateService.ValidateAsync(request);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Manage")]
    [HttpPost("send-test")]
    public async Task<IActionResult> SendTest([FromBody] SendEmailTemplateTestRequest request)
    {
        var response = await _emailTemplateService.SendTestAsync(request);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Manage")]
    [HttpPost("auto-translate")]
    public async Task<IActionResult> AutoTranslate([FromBody] AutoTranslateEmailTemplateRequest request)
    {
        var response = await _emailTemplateService.AutoTranslateAsync(request);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Manage")]
    [HttpPost("auto-translate-culture")]
    public async Task<IActionResult> AutoTranslateCulture([FromBody] AutoTranslateCultureRequest request)
    {
        var response = await _emailTemplateService.AutoTranslateCultureAsync(
            request.SourceCulture,
            request.TargetCulture,
            request.OverwriteExisting);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Email", "Manage")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var response = await _emailTemplateService.DeleteAsync(id);
        return FromResponse(response);
    }
}

public class AutoTranslateCultureRequest
{
    public string SourceCulture { get; set; } = string.Empty;
    public string TargetCulture { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; }
}
