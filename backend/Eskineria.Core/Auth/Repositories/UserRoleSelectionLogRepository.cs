using Eskineria.Core.Auth.Abstractions;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Eskineria.Core.Auth.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Auth.Repositories;

public sealed class UserRoleSelectionLogRepository
    : EfRepository<DbContext, UserRoleSelectionLog>, IRoleSelectionAuditStore
{
    public UserRoleSelectionLogRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }

    public async Task RecordSelectionAsync(
        Guid userId,
        string? previousRole,
        string newRole,
        DateTime changedAtUtc,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        await AddAsync(new UserRoleSelectionLog
        {
            UserId = userId,
            PreviousRole = previousRole,
            NewRole = newRole,
            ChangedAt = changedAtUtc,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        await SaveChangesAsync();
    }
}
