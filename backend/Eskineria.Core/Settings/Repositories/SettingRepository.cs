using Eskineria.Core.Settings.Abstractions;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Eskineria.Core.Settings.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Settings.Repositories;

public sealed class SettingRepository
    : EfRepository<DbContext, Setting>, ISettingRepository
{
    public SettingRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
