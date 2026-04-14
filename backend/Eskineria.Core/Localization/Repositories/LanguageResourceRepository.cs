using Eskineria.Core.Localization.Abstractions;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Eskineria.Core.Localization.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Localization.Repositories;

public sealed class LanguageResourceRepository
    : EfRepository<DbContext, LanguageResource>, ILanguageResourceRepository
{
    public LanguageResourceRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
