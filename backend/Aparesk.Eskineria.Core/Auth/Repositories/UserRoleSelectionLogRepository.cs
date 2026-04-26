using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Aparesk.Eskineria.Core.Auth.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Auth.Repositories;

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
