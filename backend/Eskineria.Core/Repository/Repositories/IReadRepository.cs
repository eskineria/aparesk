using System.Linq.Expressions;
using Eskineria.Core.Repository.Paging;
using Eskineria.Core.Repository.Specification;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Repository.Repositories;

public interface IReadRepository<TContext, TEntity> 
    : IEntityReadRepository<TEntity>
    where TContext : DbContext
    where TEntity : class
{
    TContext DbContext { get; }
}
