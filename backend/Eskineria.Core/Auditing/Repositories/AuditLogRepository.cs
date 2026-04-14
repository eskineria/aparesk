using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Auditing.Models;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Auditing.Repositories;

public sealed class AuditLogRepository
    : EfRepository<DbContext, AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
