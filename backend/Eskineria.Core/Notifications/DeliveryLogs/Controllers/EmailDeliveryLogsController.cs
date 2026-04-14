using Eskineria.Core.Notifications.DeliveryLogs.Abstractions;
using Eskineria.Core.Notifications.DeliveryLogs.Models;
using Eskineria.Core.Auth.Authorization;
using Eskineria.Core.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eskineria.Core.Notifications.DeliveryLogs.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class EmailDeliveryLogsController : ApiControllerBase
{
    private readonly IEmailDeliveryLogService _emailDeliveryLogService;

    public EmailDeliveryLogsController(
        IEmailDeliveryLogService emailDeliveryLogService)
    {
        _emailDeliveryLogService = emailDeliveryLogService;
    }

    [Authorize]
    [HasPermission("Email", "Read")]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] GetEmailDeliveryLogsRequest request, CancellationToken cancellationToken)
    {
        var response = await _emailDeliveryLogService.GetPagedAsync(request, cancellationToken);
        return FromResponse(response);
    }
}
