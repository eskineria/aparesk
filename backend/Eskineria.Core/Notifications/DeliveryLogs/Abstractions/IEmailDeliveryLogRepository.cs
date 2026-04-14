using Eskineria.Core.Repository.Repositories;
using Eskineria.Core.Notifications.DeliveryLogs.Entities;

namespace Eskineria.Core.Notifications.DeliveryLogs.Abstractions;

public interface IEmailDeliveryLogRepository : IEntityReadRepository<EmailDeliveryLog>;
