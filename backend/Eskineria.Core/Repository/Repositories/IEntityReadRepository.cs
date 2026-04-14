using System.Linq.Expressions;
using Eskineria.Core.Repository.Paging;
using Eskineria.Core.Repository.Specification;

namespace Eskineria.Core.Repository.Repositories;

public interface IEntityReadRepository<TEntity>
    where TEntity : class
{
    IQueryable<TEntity> Query(bool asNoTracking = true);
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetListAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
    Task<IPaginate<TEntity>> GetPagedListAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
}
