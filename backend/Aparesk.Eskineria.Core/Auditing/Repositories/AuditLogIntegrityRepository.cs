using Aparesk.Eskineria.Core.Auditing.Abstractions;
using Aparesk.Eskineria.Core.Auditing.Models;
using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Auditing.Repositories;

public sealed class AuditLogIntegrityRepository
    : EfRepository<DbContext, AuditLogIntegrity>, IAuditLogIntegrityRepository
{
    public AuditLogIntegrityRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
