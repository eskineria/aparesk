using Microsoft.AspNetCore.Identity;

namespace Aparesk.Eskineria.Core.Auth.Entities;

public class EskineriaRole : IdentityRole<Guid>
{
    public EskineriaRole() : base() { }
    public EskineriaRole(string roleName) : base(roleName) { }
}
