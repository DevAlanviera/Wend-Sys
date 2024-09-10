using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    public class ListByFiltersProductPresentationSpecification : BaseSpecification<ProductPresentation>
    {
        public ListByFiltersProductPresentationSpecification(Dictionary<string, int?> filter) : base(c => true)
        {
            if (filter["ProductId"].HasValue)
                AppendCriteria(c => c.ProductId == filter["ProductId"].Value, true);

            if (filter["PresentationId"].HasValue)
                AppendCriteria(c => c.PresentationId == filter["PresentationId"].Value, true);

            AddInclude(c => c.Product);
            AddInclude(c => c.Presentation);
            AddInclude(c => c.Movements);
        }
    }
}
