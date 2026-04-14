using System.Linq.Expressions;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Paging;
using Eskineria.Core.Repository.Specification;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Core.Repository.Repositories;

public class EfRepository<TContext, TEntity> : IRepository<TContext, TEntity>, IEntityRepository<TEntity>
    where TContext : DbContext
    where TEntity : class
{
    protected readonly TContext Context;
    protected readonly DbSet<TEntity> DbSet;
    private readonly RepositoryOptions _options;

    [Obsolete("Use constructor overload with RepositoryOptions to ensure configured options are applied.", false)]
    public EfRepository(TContext context)
        : this(context, new RepositoryOptions())
    {
    }

    public EfRepository(TContext context, RepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        Context = context;
        DbSet = Context.Set<TEntity>();
        _options = options;
    }

    public TContext DbContext => Context;

    public IQueryable<TEntity> Query(bool asNoTracking = true)
    {
        var query = DbSet.AsQueryable();
        return asNoTracking ? query.AsNoTracking() : query;
    }

    public virtual async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await DbSet.FindAsync(new[] { id }, cancellationToken);
    }

    public virtual async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return await ApplyTracking(DbSet).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetListAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var query = SpecificationEvaluator<TEntity>.GetQuery(DbSet.AsQueryable(), spec);
        return await ApplyTracking(query).ToListAsync(cancellationToken);
    }

    public virtual async Task<IPaginate<TEntity>> GetPagedListAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var query = BuildQueryWithoutPaging(spec);

        var count = await query.CountAsync(cancellationToken);

        var take = spec.Take <= 0 ? _options.DefaultPageSize : spec.Take;
        take = Math.Clamp(take, 1, _options.MaxPageSize);
        var skip = Math.Max(spec.Skip, 0);

        // Apply sorting
        if (spec.OrderBy != null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending != null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.IsPagingEnabled)
        {
            query = query.Skip(skip).Take(take);
        }

        // Includes
        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Apply tracking at the very end, after shaping.
        query = ApplyTracking(query);

        var items = await query.ToListAsync(cancellationToken);

        var effectiveSize = spec.IsPagingEnabled ? take : Math.Max(count, 1);
        var index = spec.IsPagingEnabled ? skip / take : 0;

        return new PagedList<TEntity>(items, index, effectiveSize, count);
    }

    protected IQueryable<TEntity> ApplyTracking(IQueryable<TEntity> query)
    {
        return _options.EnableNoTracking ? query.AsNoTracking() : query;
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await DbSet.AddAsync(entity, cancellationToken);
        if (_options.AutoSave)
            await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        await DbSet.AddRangeAsync(entities, cancellationToken);
        if (_options.AutoSave)
            await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.Entry(entity).State = EntityState.Modified;
        if (_options.AutoSave)
            await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Remove(entity);
        if (_options.AutoSave)
            await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        DbSet.RemoveRange(entities);
        if (_options.AutoSave)
            await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TEntity> BuildQueryWithoutPaging(ISpecification<TEntity> spec)
    {
        var query = DbSet.AsQueryable();

        if (spec.IsIgnoreQueryFiltersEnabled)
        {
            query = query.IgnoreQueryFilters();
        }

        if (spec.Criteria != null)
        {
            query = query.Where(spec.Criteria);
        }

        return query;
    }
}
