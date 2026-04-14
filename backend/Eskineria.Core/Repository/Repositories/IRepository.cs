using System.Linq.Expressions;
using Eskineria.Core.Repository.Paging;
using Eskineria.Core.Repository.Specification;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Repository.Repositories;

public interface IRepository<TContext, TEntity> : IReadRepository<TContext, TEntity> 
    , IEntityRepository<TEntity>
    where TContext : DbContext
    where TEntity : class
{
}
