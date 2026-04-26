using Aparesk.Eskineria.Core.Notifications.Models;

namespace Aparesk.Eskineria.Core.Notifications.Abstractions;

public interface INotificationDeliveryPersistence
{
    Task SaveAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default);
}
