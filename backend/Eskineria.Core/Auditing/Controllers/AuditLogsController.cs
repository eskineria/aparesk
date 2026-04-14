using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Auditing.Requests;
using Eskineria.Core.Auth.Authorization;
using Eskineria.Core.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eskineria.Core.Auditing.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class AuditLogsController : ApiControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [Authorize]
    [HasPermission("Audit", "Read")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] GetAuditLogsRequest request, CancellationToken cancellationToken)
    {
        var response = await _auditLogService.GetPagedAsync(request, cancellationToken);
        return FromResponse(response);
    }

    [Authorize]
    [HasPermission("Audit", "Read")]
    [HttpGet("filters")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken)
    {
        var response = await _auditLogService.GetFilterOptionsAsync(cancellationToken);
        return FromDataOnlyResponse(response);
    }

    [Authorize]
    [HasPermission("Audit", "Read")]
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(CancellationToken cancellationToken)
    {
        var response = await _auditLogService.GetAlertsAsync(cancellationToken);
        return FromDataOnlyResponse(response);
    }

    [Authorize]
    [HasPermission("Audit", "Read")]
    [HttpGet("integrity")]
    public async Task<IActionResult> GetIntegritySummary(CancellationToken cancellationToken)
    {
        var response = await _auditLogService.GetIntegritySummaryAsync(cancellationToken);
        return FromDataOnlyResponse(response);
    }

    [Authorize]
    [HasPermission("Audit", "Read")]
    [HttpGet("{id:long}/diff")]
    public async Task<IActionResult> GetDiff(long id, CancellationToken cancellationToken)
    {
        var response = await _auditLogService.GetDiffAsync(id, cancellationToken);
        return FromDataOnlyResponse(response);
    }
}
