using Aparesk.Eskineria.Core.Notifications.Email;
using Microsoft.EntityFrameworkCore;
using Aparesk.Eskineria.Core.Notifications.Templates.Entities;

namespace Aparesk.Eskineria.Core.Notifications.Templates.Repositories;

public sealed class EfEmailTemplatePersistence : IEmailTemplatePersistence
{
    private readonly DbContext _dbContext;

    public EfEmailTemplatePersistence(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ActiveEmailTemplateDescriptor>> GetActiveTemplatesAsync(
        string key,
        IReadOnlyCollection<string> cultures,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<EmailTemplate>()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.Key == key &&
                x.PublishedVersion != null &&
                cultures.Contains(x.Culture))
            .Select(x => new ActiveEmailTemplateDescriptor
            {
                Id = x.Id,
                Key = x.Key,
                Culture = x.Culture,
                Version = x.PublishedVersion!.Value,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<EmailTemplateContent?> GetRevisionAsync(
        int templateId,
        string key,
        string culture,
        int version,
        string trackingKey,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<EmailTemplateRevision>()
            .AsNoTracking()
            .Where(x => x.EmailTemplateId == templateId && x.Version == version)
            .Select(x => new EmailTemplateContent
            {
                Key = key,
                TrackingKey = trackingKey,
                Culture = culture,
                Version = version,
                Subject = x.Subject,
                Body = x.Body
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
