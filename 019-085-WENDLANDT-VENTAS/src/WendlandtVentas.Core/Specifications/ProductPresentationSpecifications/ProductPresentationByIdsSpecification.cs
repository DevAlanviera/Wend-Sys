using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    public class ProductPresentationByIdsSpecification : BaseSpecification<ProductPresentation>
    {
        public ProductPresentationByIdsSpecification(IEnumerable<int> ids) : base(c => ids.Any(d => d == c.Id))
        {
            AddInclude(c => c.Product);
        }
    }
}
