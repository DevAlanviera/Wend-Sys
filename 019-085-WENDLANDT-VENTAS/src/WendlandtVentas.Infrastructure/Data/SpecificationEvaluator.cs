using LinqKit;
using Microsoft.EntityFrameworkCore;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System.Linq;

namespace WendlandtVentas.Infrastructure.Data
{
    public class SpecificationEvaluator<T> where T : BaseEntity
    {
        public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
        {
            var query = inputQuery;
            var predicate = PredicateBuilder.New<T>();

            // modify the IQueryable using the specification's criteria expression
            if (specification.Criteria != null)
            {
                predicate.And(specification.Criteria);
            }

            if (specification.CriteriasToAppend.Any())
            {
                foreach (var criteria in specification.CriteriasToAppend)
                {
                    if (criteria.isAnd)
                        predicate.And(criteria.expression);
                    else
                        predicate.Or(criteria.expression);
                }
            }

            query = query.Where(predicate);

            // Includes all expression-based includes
            query = specification.Includes.Aggregate(query,
                (current, include) => current.Include(include));

            // Include any string-based include statements
            query = specification.IncludeStrings.Aggregate(query,
                (current, include) => current.Include(include));

            // Apply ordering if expressions are set
            if (specification.OrderBy != null)
            {
                query = query.OrderBy(specification.OrderBy);
            }
            else if (specification.OrderByDescending != null)
            {
                query = query.OrderByDescending(specification.OrderByDescending);
            }

            // Apply paging if enabled
            if (specification.IsPagingEnabled)
            {
                query = query.Skip(specification.Skip)
                    .Take(specification.Take);
            }
            return query;
        }
    }
}