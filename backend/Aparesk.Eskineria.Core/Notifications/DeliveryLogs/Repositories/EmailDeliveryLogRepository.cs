using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Abstractions;
using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Repositories;

public sealed class EmailDeliveryLogRepository
    : EfRepository<DbContext, EmailDeliveryLog>, IEmailDeliveryLogRepository
{
    public EmailDeliveryLogRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
