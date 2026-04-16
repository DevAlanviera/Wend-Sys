using Ardalis.Specification;
using WendlandtVentas.Core.Entities;


namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    public class ProductPresentationWithProductSpec : Specification<ProductPresentation>
    {
        public ProductPresentationWithProductSpec(int id)
        {
            Query
            .Where(pp => pp.Id == id)
            .Include(pp => pp.Product);
        }
    }
}
