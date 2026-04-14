using Eskineria.Core.Auditing.Models;
using Eskineria.Core.Repository.Repositories;

namespace Eskineria.Core.Auditing.Abstractions;

public interface IAuditLogIntegrityRepository : IEntityReadRepository<AuditLogIntegrity>;
