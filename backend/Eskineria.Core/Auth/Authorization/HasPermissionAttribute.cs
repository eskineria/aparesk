using Microsoft.AspNetCore.Authorization;

namespace Eskineria.Core.Auth.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string module, string category, string action)
    {
        Permission = $"{module}.{category}.{action}";
        Policy = Permission;
    }

    public HasPermissionAttribute(string module, string action)
    {
        Permission = $"{module}.{action}";
        Policy = Permission;
    }

    public string Permission { get; }
}
