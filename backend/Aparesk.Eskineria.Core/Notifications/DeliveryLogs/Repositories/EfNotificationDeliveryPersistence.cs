using Aparesk.Eskineria.Core.Notifications.Abstractions;
using Aparesk.Eskineria.Core.Notifications.Models;
using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Repositories;

public sealed class EfNotificationDeliveryPersistence : INotificationDeliveryPersistence
{
    private readonly DbContext _context;

    public EfNotificationDeliveryPersistence(DbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        _context.Set<EmailDeliveryLog>().Add(new EmailDeliveryLog
        {
            Channel = string.IsNullOrWhiteSpace(record.Channel) ? "Email" : record.Channel.Trim(),
            Recipient = TrimOrNull(record.Recipient, 500) ?? string.Empty,
            Subject = TrimOrNull(record.Subject, 500) ?? string.Empty,
            TemplateKey = TrimOrNull(record.TemplateKey, 150),
            Culture = TrimOrNull(record.Culture, 10),
            Status = TrimOrNull(record.Status, 40),
            ProviderName = TrimOrNull(record.ProviderName, 100),
            MessageId = TrimOrNull(record.MessageId, 200),
            ErrorMessage = TrimOrNull(record.ErrorMessage, 2000),
            CorrelationId = TrimOrNull(record.CorrelationId, 128),
            RequestedByUserId = TrimOrNull(record.RequestedByUserId, 128),
            CreatedAt = record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
