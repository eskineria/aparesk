namespace Aparesk.Eskineria.Core.Auth.Entities;

public class UserRoleSelectionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? PreviousRole { get; set; }
    public string NewRole { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
