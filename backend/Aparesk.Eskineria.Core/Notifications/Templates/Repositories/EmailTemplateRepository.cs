using Aparesk.Eskineria.Core.Notifications.Templates.Abstractions;
using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Aparesk.Eskineria.Core.Notifications.Templates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Notifications.Templates.Repositories;

public sealed class EmailTemplateRepository
    : EfRepository<DbContext, EmailTemplate>, IEmailTemplateRepository
{
    public EmailTemplateRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
