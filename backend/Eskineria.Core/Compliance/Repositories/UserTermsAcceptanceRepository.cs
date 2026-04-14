using Eskineria.Core.Compliance.Abstractions;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Eskineria.Core.Compliance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Compliance.Repositories;

public sealed class UserTermsAcceptanceRepository
    : EfRepository<DbContext, UserTermsAcceptance>, IUserTermsAcceptanceRepository
{
    public UserTermsAcceptanceRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
