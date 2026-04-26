namespace Aparesk.Eskineria.Core.Notifications.Email;

public interface IEmailTemplatePersistence
{
    Task<IReadOnlyList<ActiveEmailTemplateDescriptor>> GetActiveTemplatesAsync(
        string key,
        IReadOnlyCollection<string> cultures,
        CancellationToken cancellationToken = default);

    Task<EmailTemplateContent?> GetRevisionAsync(
        int templateId,
        string key,
        string culture,
        int version,
        string trackingKey,
        CancellationToken cancellationToken = default);
}

public sealed class ActiveEmailTemplateDescriptor
{
    public int Id { get; init; }
    public required string Key { get; init; }
    public required string Culture { get; init; }
    public int Version { get; init; }
}
