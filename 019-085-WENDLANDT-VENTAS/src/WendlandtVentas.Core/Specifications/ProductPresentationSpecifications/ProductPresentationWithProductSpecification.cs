using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    public class ProductPresentationWithProductSpecification : BaseSpecification<ProductPresentation>
    {
        public ProductPresentationWithProductSpecification(int id) : base(c => c.Id == id)
        {
            AddInclude(c => c.Product);
        }
    }
}
