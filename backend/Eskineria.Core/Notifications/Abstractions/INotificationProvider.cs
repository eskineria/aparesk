using Eskineria.Core.Notifications.Models;

namespace Eskineria.Core.Notifications.Abstractions;

public interface INotificationProvider : INotificationService
{
    bool CanHandle(NotificationChannel channel);
}
