using System.Security.Claims;
using Aparesk.Eskineria.Core.Auth.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Aparesk.Eskineria.Core.Auth.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(Permissions.AdminRole))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (HasPermission(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        return user.Claims.Any(claim =>
            claim.Type == Permissions.ClaimType &&
            string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
    }
}
