namespace Aparesk.Eskineria.Core.Notifications.Email;

public class NullEmailTemplateProvider : IEmailTemplateProvider
{
    public Task<EmailTemplateContent?> GetActiveTemplateAsync(
        string key,
        string? recipient = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<EmailTemplateContent?>(null);
    }
}
