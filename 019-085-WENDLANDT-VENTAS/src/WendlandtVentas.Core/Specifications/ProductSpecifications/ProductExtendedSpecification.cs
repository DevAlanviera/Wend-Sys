using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Specifications;

namespace Monobits.Core.Specifications
{
    public class ProductExtendedSpecification : BaseSpecification<Product>
    {
        public ProductExtendedSpecification(int productId) : base(c => c.Id == productId)
        {
            AddInclude($"{nameof(Product.ProductPresentations)}.{nameof(ProductPresentation.Movements)}");
            AddInclude($"{nameof(Product.ProductPresentations)}.{nameof(ProductPresentation.Presentation)}");
            // NUEVO: Incluimos el producto maestro para saber su nombre
            AddInclude(nameof(Product.InventorySource));
            // --- ASEGÚRATE DE TENER ESTA LÍNEA TAMBIÉN ---
            AddInclude(p => p.BundleComponents);
        }

        public ProductExtendedSpecification() : base(c => true)
        {
            AddInclude($"{nameof(Product.ProductPresentations)}.{nameof(ProductPresentation.Movements)}");
            AddInclude($"{nameof(Product.ProductPresentations)}.{nameof(ProductPresentation.Presentation)}");
            // NUEVO: Incluimos el producto maestro para la tabla general
            AddInclude(nameof(Product.InventorySource));
            // --- ASEGÚRATE DE TENER ESTA LÍNEA TAMBIÉN ---
            AddInclude(p => p.BundleComponents);
        }
    }
}
