using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductSpecifications
{
    public class ProductByIdsSpecification : BaseSpecification<Product>
    {
        public ProductByIdsSpecification(IEnumerable<int> ids) : base(c => ids.Any(d => d == c.Id))
        {            
        }
    }
}
