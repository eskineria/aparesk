using Aparesk.Eskineria.Core.Settings.Abstractions;
using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Aparesk.Eskineria.Core.Settings.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Settings.Repositories;

public sealed class SettingRepository
    : EfRepository<DbContext, Setting>, ISettingRepository
{
    public SettingRepository(DbContext context, RepositoryOptions options)
        : base(context, options)
    {
    }
}
