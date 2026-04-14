using Eskineria.Core.Notifications.Models;

namespace Eskineria.Core.Notifications.Abstractions;

public interface INotificationDeliveryPersistence
{
    Task SaveAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default);
}
