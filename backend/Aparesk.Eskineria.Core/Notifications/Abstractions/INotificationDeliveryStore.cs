using Aparesk.Eskineria.Core.Notifications.Models;

namespace Aparesk.Eskineria.Core.Notifications.Abstractions;

public interface INotificationDeliveryStore
{
    Task SaveAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default);
}
