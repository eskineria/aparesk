namespace Eskineria.Core.Notifications.Email;

public interface IEmailTemplateProvider
{
    Task<EmailTemplateContent?> GetActiveTemplateAsync(
        string key,
        string? recipient = null,
        CancellationToken cancellationToken = default);
}
