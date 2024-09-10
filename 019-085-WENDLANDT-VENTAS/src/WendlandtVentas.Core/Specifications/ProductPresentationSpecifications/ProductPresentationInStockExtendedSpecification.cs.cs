using System.Linq;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    public class ProductPresentationInStockExtendedSpecification : BaseSpecification<ProductPresentation>
    {
        public ProductPresentationInStockExtendedSpecification() : base(c => true)
        {            
            AddInclude(c => c.Movements);
            AddInclude(c => c.Product);
            AddInclude(c => c.Presentation);
            AppendCriteria(c => c.Movements != null && c.Movements.Any(c => !c.IsDeleted) && c.Movements.Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedAt).First().Quantity > 0, true);
        }
    }
}
