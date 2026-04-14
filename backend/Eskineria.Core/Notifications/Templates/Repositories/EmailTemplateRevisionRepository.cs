using Eskineria.Core.Notifications.Templates.Abstractions;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Eskineria.Core.Notifications.Templates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Notifications.Templates.Repositories;

public sealed class EmailTemplateRevisionRepository
    : EfRepository<DbContext, EmailTemplateRevision>, IEmailTemplateRevisionRepository
{
    public EmailTemplateRevisionRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
