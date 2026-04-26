using Aparesk.Eskineria.Core.Notifications.Models;

namespace Aparesk.Eskineria.Core.Notifications.Abstractions;

public interface INotificationProvider : INotificationService
{
    bool CanHandle(NotificationChannel channel);
}
