using System;
using System.Collections.Generic;
using System.Text;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductSpecifications
{
    public class ProductTableSpecification : BaseSpecification<Product>
    {
        // Para el listado general (Pestañas 1 y 2)
        public ProductTableSpecification() : base(c => true)
        {
            // Solo incluimos la presentación para obtener nombres y precios
            AddInclude($"{nameof(Product.ProductPresentations)}.{nameof(ProductPresentation.Presentation)}");
            AddInclude(nameof(Product.InventorySource));
        }
    }
}
