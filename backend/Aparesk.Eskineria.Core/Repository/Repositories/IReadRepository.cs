using System.Linq.Expressions;
using Aparesk.Eskineria.Core.Repository.Paging;
using Aparesk.Eskineria.Core.Repository.Specification;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Repository.Repositories;

public interface IReadRepository<TContext, TEntity> 
    : IEntityReadRepository<TEntity>
    where TContext : DbContext
    where TEntity : class
{
    TContext DbContext { get; }
}
