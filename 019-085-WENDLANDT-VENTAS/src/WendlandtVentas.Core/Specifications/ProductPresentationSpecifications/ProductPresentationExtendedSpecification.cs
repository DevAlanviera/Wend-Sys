using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    public class ProductPresentationExtendedSpecification : BaseSpecification<ProductPresentation>
    {
        public ProductPresentationExtendedSpecification(int id) : base(c => c.Id == id)
        {
            
            AddInclude(c => c.Product);
            AddInclude(c => c.Presentation);
            // Agrega esta línea para cargar los lotes en la consulta por ID
            AddInclude(c => c.Batches);
            AddInclude(c => c.Movements);
        }

        public ProductPresentationExtendedSpecification() : base(c => true)
        {
            
            AddInclude(c => c.Product);
            AddInclude(c => c.Presentation);
            // Agrega esta línea también aquí para las consultas generales
            AddInclude(c => c.Batches);
            AddInclude(c => c.Movements);
        }
    }
}
