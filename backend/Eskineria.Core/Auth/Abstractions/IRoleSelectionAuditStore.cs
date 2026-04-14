namespace Eskineria.Core.Auth.Abstractions;

public interface IRoleSelectionAuditStore
{
    Task RecordSelectionAsync(
        Guid userId,
        string? previousRole,
        string newRole,
        DateTime changedAtUtc,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
