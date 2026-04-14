using Eskineria.Core.Compliance.Abstractions;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Eskineria.Core.Compliance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Compliance.Repositories;

public sealed class TermsRepository
    : EfRepository<DbContext, TermsAndConditions>, ITermsRepository
{
    public TermsRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
