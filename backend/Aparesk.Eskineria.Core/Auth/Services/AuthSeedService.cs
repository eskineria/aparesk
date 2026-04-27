using System.Security.Claims;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Auth.Constants;
using Aparesk.Eskineria.Core.Auth.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Aparesk.Eskineria.Core.Auth.Services;

public sealed class AuthSeedService
{
    private static readonly string[] AdminBaseScreenPermissions =
    [
        Permissions.UsersRead,
        Permissions.UsersManage,
        Permissions.RolesRead,
        Permissions.RolesManage,
        Permissions.SettingsRead,
        Permissions.SettingsManage,
        Permissions.ComplianceRead,
        Permissions.ComplianceManage,
        Permissions.LocalizationRead,
        Permissions.LocalizationManage,
        Permissions.AuditRead,
        Permissions.EmailRead,
        Permissions.EmailManage,
        Permissions.DashboardView,
        Permissions.SitesRead,
        Permissions.SitesManage,
        Permissions.BlocksRead,
        Permissions.BlocksManage,
        Permissions.UnitsRead,
        Permissions.UnitsManage,
        Permissions.ResidentsRead,
        Permissions.ResidentsManage
    ];

    private static readonly string[] ManagerBaseScreenPermissions =
    [
        Permissions.UsersRead,
        Permissions.UsersManage,
        Permissions.RolesRead,
        Permissions.RolesManage,
        Permissions.SettingsRead,
        Permissions.SettingsManage,
        Permissions.ComplianceRead,
        Permissions.ComplianceManage,
        Permissions.LocalizationRead,
        Permissions.LocalizationManage,
        Permissions.AuditRead,
        Permissions.EmailRead,
        Permissions.EmailManage,
        Permissions.DashboardView,
        Permissions.SitesRead,
        Permissions.SitesManage,
        Permissions.BlocksRead,
        Permissions.BlocksManage,
        Permissions.UnitsRead,
        Permissions.UnitsManage,
        Permissions.ResidentsRead,
        Permissions.ResidentsManage
    ];

    private readonly UserManager<EskineriaUser> _userManager;
    private readonly RoleManager<EskineriaRole> _roleManager;
    private readonly IPermissionDiscoveryService _permissionDiscoveryService;

    public AuthSeedService(
        UserManager<EskineriaUser> userManager,
        RoleManager<EskineriaRole> roleManager,
        IPermissionDiscoveryService permissionDiscoveryService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _permissionDiscoveryService = permissionDiscoveryService;
    }

    public async Task<bool> TryHandleSeedUserRolesCommandAsync(string[] args)
    {
        if (!args.Contains("--seed-user-roles"))
        {
            return false;
        }

        var allPermissions = _permissionDiscoveryService.GetAllPermissions().ToList();

        string? GetArgValue(string name)
        {
            var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index + 1 < args.Length
                ? args[index + 1]
                : null;
        }

        var email = GetArgValue("--email");
        var rolesArg = GetArgValue("--roles");
        var activeRole = GetArgValue("--active-role");
        var password = GetArgValue("--password");
        var firstName = GetArgValue("--first-name") ?? "System";
        var lastName = GetArgValue("--last-name") ?? "Admin";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(rolesArg))
        {
            Console.WriteLine("Usage: --seed-user-roles --email user@example.com --roles Admin,Manager [--active-role Admin] [--password StrongPass123] [--first-name System] [--last-name Admin]");
            return true;
        }

        var roles = rolesArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (roles.Length == 0)
        {
            Console.WriteLine("No roles provided.");
            return true;
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine($"User not found: {email}. Provide --password to create the user.");
                return true;
            }

            user = new EskineriaUser
            {
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var createUserResult = await _userManager.CreateAsync(user, password);
            if (!createUserResult.Succeeded)
            {
                Console.WriteLine($"Failed to create user {email}: {string.Join("; ", createUserResult.Errors.Select(e => e.Description))}");
                return true;
            }

            Console.WriteLine($"User created: {email}");
        }

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var createResult = await _roleManager.CreateAsync(new EskineriaRole(roleName));
                if (!createResult.Succeeded)
                {
                    Console.WriteLine($"Failed to create role {roleName}: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
                    return true;
                }
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToAdd = roles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();
        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                Console.WriteLine($"Failed to add roles: {string.Join("; ", addResult.Errors.Select(e => e.Description))}");
                return true;
            }
        }

        user.ActiveRole = !string.IsNullOrWhiteSpace(activeRole) ? activeRole : roles[0];
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            Console.WriteLine($"Failed to set active role: {string.Join("; ", updateResult.Errors.Select(e => e.Description))}");
            return true;
        }

        foreach (var roleName in roles)
        {
            await SynchronizeRolePermissionsAsync(roleName, allPermissions);
        }

        Console.WriteLine($"Roles added to {email}: {string.Join(", ", roles)}. ActiveRole: {user.ActiveRole}");
        Console.WriteLine("Permissions synchronized for Admin and Manager roles.");
        return true;
    }

    public async Task SeedStartupAsync(IConfiguration configuration)
    {
        var allPermissions = _permissionDiscoveryService.GetAllPermissions().ToList();

        var configuredRoles = configuration.GetSection("StartupSeed:Roles").Get<string[]>() ?? Array.Empty<string>();
        var rolesToSeed = configuredRoles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var roleName in rolesToSeed)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var createRoleResult = await _roleManager.CreateAsync(new EskineriaRole(roleName));
                if (!createRoleResult.Succeeded)
                {
                    Console.WriteLine($"[StartupSeed] Failed to create role {roleName}: {string.Join("; ", createRoleResult.Errors.Select(e => e.Description))}");
                    continue;
                }
            }

            await SynchronizeRolePermissionsAsync(roleName, allPermissions);
        }

        var usersSection = configuration.GetSection("StartupSeed:Users");
        if (usersSection.Exists())
        {
            foreach (var userConfig in usersSection.GetChildren())
            {
                await SeedUserFromConfigAsync(userConfig, allPermissions);
            }
        }
        else
        {
            // Fallback to legacy Admin section for backward compatibility
            var adminSection = configuration.GetSection("StartupSeed:Admin");
            if (adminSection.Exists())
            {
                await SeedUserFromConfigAsync(adminSection, allPermissions);
            }
        }
    }

    private async Task SeedUserFromConfigAsync(IConfigurationSection config, List<string> allPermissions)
    {
        var email = config["Email"];
        var password = config["Password"];
        var firstName = config["FirstName"] ?? "System";
        var lastName = config["LastName"] ?? "User";
        var activeRole = config["ActiveRole"];
        var roles = config.GetSection("Roles").Get<string[]>() ?? Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                password.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[StartupSeed] User not created. Provide Password for {email}.");
                return;
            }

            user = new EskineriaUser
            {
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                Console.WriteLine($"[StartupSeed] Failed to create user {email}: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
                return;
            }

            Console.WriteLine($"[StartupSeed] User created: {email}");
        }
        else if (!string.IsNullOrWhiteSpace(password) && !password.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase))
        {
            // Ensure seeded user's password matches configuration even if user already exists
            var checkPassword = await _userManager.CheckPasswordAsync(user, password);
            if (!checkPassword)
            {
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (removeResult.Succeeded)
                {
                    await _userManager.AddPasswordAsync(user, password);
                    Console.WriteLine($"[StartupSeed] Password reset for seeded user: {email}");
                }
            }
        }

        var userRoles = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (userRoles.Length > 0)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = userRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

            if (rolesToAdd.Count > 0)
            {
                var addRolesResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addRolesResult.Succeeded)
                {
                    Console.WriteLine($"[StartupSeed] Failed to add roles for {email}: {string.Join("; ", addRolesResult.Errors.Select(e => e.Description))}");
                }
            }

            user.ActiveRole = !string.IsNullOrWhiteSpace(activeRole) ? activeRole : userRoles[0];
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                Console.WriteLine($"[StartupSeed] Failed to set active role for {email}: {string.Join("; ", updateResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    private async Task SynchronizeRolePermissionsAsync(string roleName, List<string> allPermissions)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return;
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var existingPermissions = existingClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var targetPermissions = GetTargetPermissions(roleName, allPermissions);

        foreach (var permission in targetPermissions)
        {
            if (!existingPermissions.Contains(permission))
            {
                await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
            }
        }

        foreach (var claim in existingClaims.Where(c => c.Type == Permissions.ClaimType))
        {
            if (!targetPermissions.Contains(claim.Value, StringComparer.OrdinalIgnoreCase))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }
        }
    }

    private static List<string> GetTargetPermissions(string roleName, List<string> allPermissions)
    {
        var targetPermissions = new List<string> { Permissions.DashboardView };

        if (string.Equals(roleName, Permissions.AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            targetPermissions.AddRange(AdminBaseScreenPermissions);
            return targetPermissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        if (string.Equals(roleName, Permissions.ManagerRole, StringComparison.OrdinalIgnoreCase))
        {
            targetPermissions.AddRange(ManagerBaseScreenPermissions);
        }
        else if (string.Equals(roleName, Permissions.UserRole, StringComparison.OrdinalIgnoreCase))
        {
            // Only dashboard (already added above) and implicitly allowed profile access
        }
        else
        {
            // For non-standard roles seeded from configuration, keep previous behavior:
            // dashboard + discovered read permissions.
            targetPermissions.AddRange(allPermissions
                .Where(p => p.EndsWith(".Read", StringComparison.OrdinalIgnoreCase)));
        }

        return targetPermissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
