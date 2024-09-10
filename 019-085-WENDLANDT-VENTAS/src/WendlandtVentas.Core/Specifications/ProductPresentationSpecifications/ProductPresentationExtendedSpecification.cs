using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    public class ProductPresentationExtendedSpecification : BaseSpecification<ProductPresentation>
    {
        public ProductPresentationExtendedSpecification(int id) : base(c => c.Id == id)
        {
            AddInclude(c => c.Movements);
            AddInclude(c => c.Product);
            AddInclude(c => c.Presentation);
        }

        public ProductPresentationExtendedSpecification() : base(c => true)
        {
            AddInclude(c => c.Movements);
            AddInclude(c => c.Product);
            AddInclude(c => c.Presentation);
        }
    }
}
