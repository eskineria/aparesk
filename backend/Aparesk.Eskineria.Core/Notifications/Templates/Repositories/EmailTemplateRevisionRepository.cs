using Aparesk.Eskineria.Core.Notifications.Templates.Abstractions;
using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Aparesk.Eskineria.Core.Notifications.Templates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Notifications.Templates.Repositories;

public sealed class EmailTemplateRevisionRepository
    : EfRepository<DbContext, EmailTemplateRevision>, IEmailTemplateRevisionRepository
{
    public EmailTemplateRevisionRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
