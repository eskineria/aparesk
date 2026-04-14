using System.Security.Claims;
using Eskineria.Core.Shared.Response;
using Microsoft.AspNetCore.Mvc;

namespace Eskineria.Core.Auth.Controllers;

public abstract class AuthApiControllerBase : ControllerBase
{
    protected IActionResult FromResponse(Response response)
    {
        return StatusCode(response.StatusCode, response);
    }

    protected IActionResult FromResponse<T>(DataResponse<T> response)
    {
        return StatusCode(response.StatusCode, response);
    }

    protected bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }
}
