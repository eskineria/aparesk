using Eskineria.Core.Notifications.DeliveryLogs.Abstractions;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Eskineria.Core.Notifications.DeliveryLogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Notifications.DeliveryLogs.Repositories;

public sealed class EmailDeliveryLogRepository
    : EfRepository<DbContext, EmailDeliveryLog>, IEmailDeliveryLogRepository
{
    public EmailDeliveryLogRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
