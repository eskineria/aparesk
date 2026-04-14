using System.Reflection;
using Eskineria.Core.Auth.Abstractions;
using Eskineria.Core.Auth.Authorization;

namespace Eskineria.Core.Auth.Services;

public sealed class PermissionDiscoveryService : IPermissionDiscoveryService
{
    private readonly IReadOnlyList<string> _permissions;

    public PermissionDiscoveryService(params Assembly[] assembliesToScan)
    {
        _permissions = DiscoverPermissions(assembliesToScan);
    }

    public IEnumerable<string> GetAllPermissions() => _permissions;

    private static IReadOnlyList<string> DiscoverPermissions(IEnumerable<Assembly> assembliesToScan)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assembliesToScan
            .Where(assembly => assembly != null)
            .Distinct())
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var attribute in type.GetCustomAttributes<HasPermissionAttribute>(inherit: true))
                {
                    permissions.Add(attribute.Permission);
                }

                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    foreach (var attribute in method.GetCustomAttributes<HasPermissionAttribute>(inherit: true))
                    {
                        permissions.Add(attribute.Permission);
                    }
                }
            }
        }

        return permissions
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
