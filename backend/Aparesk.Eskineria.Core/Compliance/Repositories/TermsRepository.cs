using Aparesk.Eskineria.Core.Compliance.Abstractions;
using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Aparesk.Eskineria.Core.Compliance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Compliance.Repositories;

public sealed class TermsRepository
    : EfRepository<DbContext, TermsAndConditions>, ITermsRepository
{
    public TermsRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
