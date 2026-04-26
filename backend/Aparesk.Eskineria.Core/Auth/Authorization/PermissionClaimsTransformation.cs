using System.Security.Claims;
using Aparesk.Eskineria.Core.Auth.Constants;
using Aparesk.Eskineria.Core.Auth.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Aparesk.Eskineria.Core.Auth.Authorization;

public sealed class PermissionClaimsTransformation : IClaimsTransformation
{
    private readonly UserManager<EskineriaUser> _userManager;
    private readonly RoleManager<EskineriaRole> _roleManager;

    public PermissionClaimsTransformation(
        UserManager<EskineriaUser> userManager,
        RoleManager<EskineriaRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
        {
            return principal;
        }

        var existingPermissions = new HashSet<string>(
            principal.Claims
                .Where(claim => claim.Type == Permissions.ClaimType)
                .Select(claim => claim.Value),
            StringComparer.OrdinalIgnoreCase);

        var claimsToAdd = new List<Claim>();

        var userClaims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in userClaims.Where(claim => claim.Type == Permissions.ClaimType))
        {
            if (existingPermissions.Add(claim.Value))
            {
                claimsToAdd.Add(claim);
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        var activeRole = principal.FindFirstValue(CustomClaimTypes.ActiveRole);
        if (string.IsNullOrWhiteSpace(activeRole))
        {
            activeRole = user.ActiveRole;
        }

        IEnumerable<string> rolesToEvaluate = roles;
        if (!string.IsNullOrWhiteSpace(activeRole) &&
            roles.Contains(activeRole, StringComparer.OrdinalIgnoreCase))
        {
            rolesToEvaluate = new[] { activeRole };
        }

        foreach (var roleName in rolesToEvaluate)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                continue;
            }

            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in roleClaims.Where(claim => claim.Type == Permissions.ClaimType))
            {
                if (existingPermissions.Add(claim.Value))
                {
                    claimsToAdd.Add(claim);
                }
            }
        }

        if (claimsToAdd.Count > 0)
        {
            var identity = new ClaimsIdentity();
            identity.AddClaims(claimsToAdd);
            principal.AddIdentity(identity);
        }

        return principal;
    }
}
