using System.Linq.Expressions;

namespace Eskineria.Core.Repository.Specification;

public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool IsIgnoreQueryFiltersEnabled { get; private set; }

    protected Specification(Expression<Func<T, bool>>? criteria)
    {
        Criteria = criteria;
    }

    protected virtual void IgnoreQueryFilters()
    {
        IsIgnoreQueryFiltersEnabled = true;
    }

    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        ArgumentNullException.ThrowIfNull(includeExpression);

        if (Includes.Contains(includeExpression))
        {
            return;
        }

        Includes.Add(includeExpression);
    }

    protected virtual void AddInclude(string includeString)
    {
        if (string.IsNullOrWhiteSpace(includeString))
        {
            return;
        }

        var normalized = includeString.Trim();
        if (IncludeStrings.Contains(normalized, StringComparer.Ordinal))
        {
            return;
        }

        IncludeStrings.Add(normalized);
    }

    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = Math.Max(skip, 0);
        Take = Math.Max(take, 0);
        IsPagingEnabled = true;
    }

    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByExpression);
        OrderBy = orderByExpression;
        OrderByDescending = null;
    }

    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByDescendingExpression);
        OrderByDescending = orderByDescendingExpression;
        OrderBy = null;
    }
}
