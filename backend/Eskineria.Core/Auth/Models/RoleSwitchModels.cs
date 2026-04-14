namespace Eskineria.Core.Auth.Models;

public class SwitchRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
}

public class RoleSwitchResultDto
{
    public string ActiveRole { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
