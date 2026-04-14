using Eskineria.Core.Shared.Response;
using Eskineria.Core.Shared.Controllers;
using Microsoft.AspNetCore.Mvc;
using Eskineria.Core.Compliance.Abstractions;
using Eskineria.Core.Compliance.Models;
using Eskineria.Core.Shared.Configuration;
using Eskineria.Core.Shared.Localization;
using Eskineria.Core.Auth.Authorization;
using Eskineria.Core.Auth.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace Eskineria.Core.Compliance.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ComplianceController : ApiControllerBase
{
    private readonly IComplianceService _complianceService;
    private readonly IStringLocalizer<ComplianceController> _localizer;

    public ComplianceController(
        IComplianceService complianceService,
        IStringLocalizer<ComplianceController> localizer)
    {
        _complianceService = complianceService;
        _localizer = localizer;
    }

    // Terms Management (Admin endpoints)
    
    [HttpGet("terms")]
    [HasPermission("Compliance", "Read")]
    [ProducesResponseType(typeof(DataResponse<List<TermsDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTerms([FromQuery] string? type = null)
    {
        var response = await _complianceService.GetAllTermsAsync(type);
        return FromResponse(response);
    }

    [HttpGet("terms/active/{type}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DataResponse<TermsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DataResponse<TermsDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveTermsByType(string type)
    {
        var response = await _complianceService.GetActiveTermsByTypeAsync(type);
        return FromResponse(response);
    }

    [HttpGet("terms/active-optional/{type}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DataResponse<TermsDto?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTermsByTypeOptional(string type)
    {
        var response = await _complianceService.GetActiveTermsByTypeAsync(type);
        if (response.Success)
        {
            return Ok(DataResponse<TermsDto?>.Succeed(response.Data, response.Message));
        }

        if (response.StatusCode == StatusCodes.Status404NotFound)
        {
            return Ok(DataResponse<TermsDto?>.Succeed(null, response.Message));
        }

        return FromResponse(response);
    }

    [HttpGet("terms/{id}")]
    [HasPermission("Compliance", "Read")]
    [ProducesResponseType(typeof(DataResponse<TermsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DataResponse<TermsDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTermsById(Guid id)
    {
        var response = await _complianceService.GetTermsByIdAsync(id);
        return FromResponse(response);
    }

    [HttpPost("terms")]
    [HasPermission("Compliance", "Manage")]
    [ProducesResponseType(typeof(DataResponse<TermsDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Eskineria.Core.Shared.Response.Response), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTerms([FromBody] CreateTermsDto dto)
    {
        var response = await _complianceService.CreateTermsAsync(dto);
        return FromResponse(response);
    }

    [HttpPut("terms/{id}")]
    [HasPermission("Compliance", "Manage")]
    [ProducesResponseType(typeof(Eskineria.Core.Shared.Response.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Eskineria.Core.Shared.Response.Response), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTerms(Guid id, [FromBody] UpdateTermsDto dto)
    {
        var response = await _complianceService.UpdateTermsAsync(id, dto);
        return FromResponse(response);
    }

    [HttpPost("terms/{id}/activate")]
    [HasPermission("Compliance", "Manage")]
    [ProducesResponseType(typeof(Eskineria.Core.Shared.Response.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Eskineria.Core.Shared.Response.Response), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateTerms(Guid id)
    {
        var response = await _complianceService.ActivateTermsAsync(id);
        return FromResponse(response);
    }

    [HttpDelete("terms/{id}")]
    [HasPermission("Compliance", "Manage")]
    [ProducesResponseType(typeof(Eskineria.Core.Shared.Response.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Eskineria.Core.Shared.Response.Response), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTerms(Guid id)
    {
        var response = await _complianceService.DeleteTermsAsync(id);
        return FromResponse(response);
    }

    // User Acceptance endpoints

    [Authorize]
    [HttpPost("accept")]
    [ProducesResponseType(typeof(Eskineria.Core.Shared.Response.Response), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Eskineria.Core.Shared.Response.Response), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptTerms([FromBody] AcceptTermsDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(Eskineria.Core.Shared.Response.Response.Fail(_localizer[LocalizationKeys.UserNotFound], StatusCodes.Status401Unauthorized));
        }

        var response = await _complianceService.AcceptTermsAsync(
            userId,
            dto.TermsAndConditionsId,
            RequestContextInfoResolver.ResolveClientIpAddress(HttpContext),
            RequestContextInfoResolver.ResolveUserAgent(HttpContext));

        return FromResponse(response);
    }

    [Authorize]
    [HttpGet("my-acceptances")]
    [ProducesResponseType(typeof(DataResponse<List<UserTermsAcceptanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAcceptances()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(Eskineria.Core.Shared.Response.Response.Fail(_localizer[LocalizationKeys.UserNotFound], StatusCodes.Status401Unauthorized));
        }

        var response = await _complianceService.GetUserAcceptancesAsync(userId);
        return FromResponse(response);
    }

    [Authorize]
    [HttpGet("check-acceptance/{type}")]
    [ProducesResponseType(typeof(DataResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckAcceptance(string type)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(Eskineria.Core.Shared.Response.Response.Fail(_localizer[LocalizationKeys.UserNotFound], StatusCodes.Status401Unauthorized));
        }

        var response = await _complianceService.HasUserAcceptedLatestTermsAsync(userId, type);
        return FromResponse(response);
    }

    [Authorize]
    [HttpGet("pending-required")]
    [ProducesResponseType(typeof(DataResponse<List<TermsDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRequiredTerms()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(Eskineria.Core.Shared.Response.Response.Fail(_localizer[LocalizationKeys.UserNotFound], StatusCodes.Status401Unauthorized));
        }

        var response = await _complianceService.GetPendingRequiredTermsAsync(userId);
        return FromResponse(response);
    }
}
