using System.Security.Claims;
using Aparesk.Eskineria.Core.Shared.Response;
using Microsoft.AspNetCore.Mvc;

namespace Aparesk.Eskineria.Core.Shared.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromResponse(Aparesk.Eskineria.Core.Shared.Response.Response response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return StatusCode(response.StatusCode, response);
    }

    protected IActionResult FromResponse<T>(DataResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return StatusCode(response.StatusCode, response);
    }

    protected IActionResult FromDataOnlyResponse<T>(DataResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.Success ? Ok(response.Data) : StatusCode(response.StatusCode, response);
    }

    protected bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        if (User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userIdValue))
        {
            return false;
        }

        return Guid.TryParse(userIdValue, out userId);
    }
}
