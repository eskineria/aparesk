using Eskineria.Core.Notifications.Abstractions;
using Eskineria.Core.Notifications.Models;
using Microsoft.Extensions.Logging;

namespace Eskineria.Core.Notifications.Providers;

public sealed class PersistentNotificationDeliveryStore : INotificationDeliveryStore
{
    private readonly INotificationDeliveryPersistence _notificationDeliveryPersistence;
    private readonly ILogger<PersistentNotificationDeliveryStore> _logger;

    public PersistentNotificationDeliveryStore(
        INotificationDeliveryPersistence notificationDeliveryPersistence,
        ILogger<PersistentNotificationDeliveryStore> logger)
    {
        _notificationDeliveryPersistence = notificationDeliveryPersistence;
        _logger = logger;
    }

    public async Task SaveAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            await _notificationDeliveryPersistence.SaveAsync(record, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist notification delivery log.");
        }
    }
}
