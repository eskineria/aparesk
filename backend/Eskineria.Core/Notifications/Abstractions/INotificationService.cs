using Eskineria.Core.Notifications.Models;

namespace Eskineria.Core.Notifications.Abstractions;

public interface INotificationService
{
    Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
