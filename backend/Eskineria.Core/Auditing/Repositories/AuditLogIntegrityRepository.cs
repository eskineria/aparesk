using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Auditing.Models;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Auditing.Repositories;

public sealed class AuditLogIntegrityRepository
    : EfRepository<DbContext, AuditLogIntegrity>, IAuditLogIntegrityRepository
{
    public AuditLogIntegrityRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
