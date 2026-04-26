using Aparesk.Eskineria.Core.Localization.Abstractions;
using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Aparesk.Eskineria.Core.Localization.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Localization.Repositories;

public sealed class LanguageResourceRepository
    : EfRepository<DbContext, LanguageResource>, ILanguageResourceRepository
{
    public LanguageResourceRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
