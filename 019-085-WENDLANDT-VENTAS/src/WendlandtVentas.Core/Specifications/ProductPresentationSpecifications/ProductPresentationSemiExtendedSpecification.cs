using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    public class ProductPresentationsSemiExtendedSpecification : BaseSpecification<ProductPresentation>
    {
        public ProductPresentationsSemiExtendedSpecification(int id) : base(c => c.Id == id)
        {
            AddInclude(c => c.Product);
            AddInclude(c => c.Presentation);
        }
        public ProductPresentationsSemiExtendedSpecification() : base(c => true)
        {
            AddInclude(c => c.Product);
            AddInclude(c => c.Presentation);
        }
    }
}
