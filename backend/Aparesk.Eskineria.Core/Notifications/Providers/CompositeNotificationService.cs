using Aparesk.Eskineria.Core.Notifications.Abstractions;
using Aparesk.Eskineria.Core.Notifications.Models;
using Aparesk.Eskineria.Core.Notifications.Utilities;
using Microsoft.Extensions.Logging;

namespace Aparesk.Eskineria.Core.Notifications.Providers;

public class CompositeNotificationService : INotificationService
{
    private readonly IReadOnlyList<INotificationProvider> _providers;
    private readonly ILogger<CompositeNotificationService> _logger;
    private readonly INotificationDeliveryStore? _notificationDeliveryStore;

    public CompositeNotificationService(
        IEnumerable<INotificationProvider> providers,
        ILogger<CompositeNotificationService> logger,
        INotificationDeliveryStore? notificationDeliveryStore = null)
    {
        _providers = (providers ?? throw new ArgumentNullException(nameof(providers))).ToList();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationDeliveryStore = notificationDeliveryStore;
    }

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        string normalizedRecipient;
        try
        {
            normalizedRecipient = NotificationSecurity.NormalizeAndValidateEmail(message.Recipient, nameof(message.Recipient));
        }
        catch (ArgumentException ex)
        {
            return NotificationResult.CreateFailure(ex.Message);
        }

        try
        {
            var providers = _providers.Where(p => p.CanHandle(message.Channel)).ToList();
            if (providers.Count == 0)
            {
                _logger.LogWarning("No notification provider registered for channel {Channel}", message.Channel);
                return NotificationResult.CreateFailure($"No notification provider available for channel {message.Channel}.");
            }

            var errors = new List<string>();
            foreach (var provider in providers)
            {
                try
                {
                    var result = await provider.SendAsync(message, cancellationToken);
                    if (result.Success)
                    {
                        await PersistDeliveryAsync(message, provider.GetType().Name, result, cancellationToken);
                        return result;
                    }

                    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        errors.Add(NotificationSecurity.SanitizeErrorMessage(result.ErrorMessage));
                    }

                    await PersistDeliveryAsync(message, provider.GetType().Name, result, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Notification provider {Provider} failed for channel {Channel}",
                        provider.GetType().Name,
                        message.Channel);
                    errors.Add(NotificationSecurity.SanitizeErrorMessage(ex.Message));

                    await PersistDeliveryAsync(
                        message,
                        provider.GetType().Name,
                        NotificationResult.CreateFailure(NotificationSecurity.SanitizeErrorMessage(ex.Message)),
                        cancellationToken);
                }
            }

            var errorMessage = errors.Count > 0
                ? string.Join(" | ", errors.Distinct(StringComparer.Ordinal))
                : $"Notification dispatch failed for channel {message.Channel}.";
            var failedResult = NotificationResult.CreateFailure(errorMessage);
            await PersistDeliveryAsync(message, nameof(CompositeNotificationService), failedResult, cancellationToken);
            return failedResult;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Notification sending cancelled for recipient {Recipient}", NotificationSecurity.MaskEmailForLog(normalizedRecipient));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to {Recipient}", NotificationSecurity.MaskEmailForLog(normalizedRecipient));
            var failedResult = NotificationResult.CreateFailure(NotificationSecurity.SanitizeErrorMessage(ex.Message));
            await PersistDeliveryAsync(message, nameof(CompositeNotificationService), failedResult, cancellationToken);
            return failedResult;
        }
    }

    private async Task PersistDeliveryAsync(
        NotificationMessage message,
        string providerName,
        NotificationResult result,
        CancellationToken cancellationToken)
    {
        if (_notificationDeliveryStore == null)
        {
            return;
        }

        try
        {
            var record = new NotificationDeliveryRecord
            {
                Channel = message.Channel.ToString(),
                Recipient = NotificationSecurity.NormalizeAndValidateEmail(message.Recipient, nameof(message.Recipient)),
                Subject = NotificationSecurity.NormalizeSubject(message.Title, nameof(message.Title)),
                ProviderName = providerName,
                Status = result.Success ? "Sent" : "Failed",
                MessageId = result.MessageId,
                ErrorMessage = NotificationSecurity.SanitizeErrorMessage(result.ErrorMessage),
                TemplateKey = GetDataValue(message, "TemplateKey"),
                Culture = GetDataValue(message, "Culture"),
                CorrelationId = GetDataValue(message, "CorrelationId"),
                RequestedByUserId = GetDataValue(message, "RequestedByUserId"),
                CreatedAt = DateTime.UtcNow,
            };

            await _notificationDeliveryStore.SaveAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist notification delivery log for recipient {Recipient}", NotificationSecurity.MaskEmailForLog(message.Recipient));
        }
    }

    private static string? GetDataValue(NotificationMessage message, string key)
    {
        if (message.Data == null)
        {
            return null;
        }

        if (!message.Data.TryGetValue(key, out var value))
        {
            return null;
        }

        return value?.ToString();
    }
}
