using System;
using System.Collections.Generic;
using System.Text;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    public class ProductBundleStockSpecification : BaseSpecification<Product>
    {
        public ProductBundleStockSpecification(int productId)
            : base(p => p.Id == productId && !p.IsDeleted)
        {
            // 1. Traemos la "receta" (los componentes del bundle)
            AddInclude(p => p.BundleComponents);

            // 2. Por cada componente, traemos el producto real
            AddInclude("BundleComponents.ComponentProduct");

            // 3. De ese producto real, traemos sus presentaciones (ej. Botella 0.355L)
            AddInclude("BundleComponents.ComponentProduct.ProductPresentations");

            // 4. De la presentación, traemos los metadatos (para saber si es Botella/Lata)
            AddInclude("BundleComponents.ComponentProduct.ProductPresentations.Presentation");

            // 5. VITAL: Traemos los lotes (Batches) para poder sumar la cantidad disponible
            AddInclude("BundleComponents.ComponentProduct.ProductPresentations.Batches");

            AddInclude("BundleComponents.ComponentProduct.ProductPresentations.Movements");
        }
    }
}
