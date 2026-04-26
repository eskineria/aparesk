using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Core.Repository.Specification;

public class SpecificationEvaluator<T> where T : class
{
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(specification);

        var query = inputQuery;
        
        if (specification.IsIgnoreQueryFiltersEnabled)
        {
            query = query.IgnoreQueryFilters();
        }

        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query,
            (current, include) => current.Include(include));

        query = specification.IncludeStrings.Aggregate(query,
            (current, include) => current.Include(include));

        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.IsPagingEnabled)
        {
            var normalizedSkip = Math.Max(specification.Skip, 0);
            var normalizedTake = Math.Max(specification.Take, 0);

            if (normalizedTake > 0)
            {
                query = query.Skip(normalizedSkip).Take(normalizedTake);
            }
        }

        return query;
    }
}
