using System.Linq.Expressions;
using Aparesk.Eskineria.Core.Repository.Paging;
using Aparesk.Eskineria.Core.Repository.Specification;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Repository.Repositories;

public interface IRepository<TContext, TEntity> : IReadRepository<TContext, TEntity> 
    , IEntityRepository<TEntity>
    where TContext : DbContext
    where TEntity : class
{
}
