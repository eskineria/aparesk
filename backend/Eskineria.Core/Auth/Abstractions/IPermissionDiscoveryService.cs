namespace Eskineria.Core.Auth.Abstractions;

public interface IPermissionDiscoveryService
{
    IEnumerable<string> GetAllPermissions();
}
