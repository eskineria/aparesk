using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Aparesk.Eskineria.Domain.Entities;
using Aparesk.Eskineria.Persistence.Features.Management.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Persistence.Features.Management.Repositories;

public sealed class BoardMemberRepository : EfRepository<DbContext, BoardMember>, IBoardMemberRepository
{
    public BoardMemberRepository(ApplicationDbContext dbContext, RepositoryOptions options)
        : base(dbContext, options)
    {
    }
}
