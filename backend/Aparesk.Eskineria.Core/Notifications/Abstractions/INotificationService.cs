using Aparesk.Eskineria.Core.Notifications.Models;

namespace Aparesk.Eskineria.Core.Notifications.Abstractions;

public interface INotificationService
{
    Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
