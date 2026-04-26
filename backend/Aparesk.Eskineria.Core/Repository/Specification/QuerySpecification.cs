using System.Linq.Expressions;

namespace Aparesk.Eskineria.Core.Repository.Specification;

public sealed class QuerySpecification<T> : Specification<T>
{
    public QuerySpecification(Expression<Func<T, bool>>? criteria = null)
        : base(criteria)
    {
    }

    public QuerySpecification<T> Include(Expression<Func<T, object>> includeExpression)
    {
        ArgumentNullException.ThrowIfNull(includeExpression);
        AddInclude(includeExpression);
        return this;
    }

    public QuerySpecification<T> Include(string includeString)
    {
        AddInclude(includeString);
        return this;
    }

    public new QuerySpecification<T> OrderBy(Expression<Func<T, object>> orderByExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByExpression);
        ApplyOrderBy(orderByExpression);
        return this;
    }

    public new QuerySpecification<T> OrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByDescendingExpression);
        ApplyOrderByDescending(orderByDescendingExpression);
        return this;
    }

    public QuerySpecification<T> Paging(int skip, int take)
    {
        ApplyPaging(skip, take);
        return this;
    }

    public QuerySpecification<T> IgnoreFiltersQuery()
    {
        IgnoreQueryFilters();
        return this;
    }
}
