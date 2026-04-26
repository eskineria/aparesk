using Aparesk.Eskineria.Core.Notifications.Abstractions;
using Aparesk.Eskineria.Core.Notifications.Email;
using Aparesk.Eskineria.Core.Notifications.Models;
using Aparesk.Eskineria.Core.Notifications.Utilities;
using Microsoft.Extensions.Logging;

namespace Aparesk.Eskineria.Core.Notifications.Providers;

public class EmailNotificationProvider : INotificationProvider
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailNotificationProvider> _logger;

    public EmailNotificationProvider(
        IEmailSender emailSender,
        ILogger<EmailNotificationProvider> logger)
    {
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanHandle(NotificationChannel channel)
    {
        return channel == NotificationChannel.Email;
    }

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!CanHandle(message.Channel))
        {
            return NotificationResult.CreateFailure("Unsupported channel for EmailNotificationProvider.");
        }

        try
        {
            var recipient = NotificationSecurity.NormalizeAndValidateEmail(message.Recipient, nameof(message.Recipient));
            var subject = NotificationSecurity.NormalizeSubject(message.Title, nameof(message.Title));
            var body = NotificationSecurity.NormalizeBody(message.Body, nameof(message.Body));

            await _emailSender.SendEmailAsync(
                recipient,
                subject,
                body,
                isHtml: true,
                cancellationToken);

            return NotificationResult.CreateSuccess(Guid.NewGuid().ToString("N"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email notification failed for recipient {Recipient}", NotificationSecurity.MaskEmailForLog(message.Recipient));
            return NotificationResult.CreateFailure(NotificationSecurity.SanitizeErrorMessage(ex.Message));
        }
    }
}
