using Aparesk.Eskineria.Core.Auditing.Models;
using Aparesk.Eskineria.Core.Repository.Repositories;

namespace Aparesk.Eskineria.Core.Auditing.Abstractions;

public interface IAuditLogRepository : IEntityReadRepository<AuditLog>;
