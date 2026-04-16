using System;
using System.Collections.Generic;
using System.Text;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductPresentationSpecifications
{
    // Esta es NUEVA, no afecta a nadie más
    public class ProductPresentationDetailsSpecification : BaseSpecification<ProductPresentation>
    {
        public ProductPresentationDetailsSpecification(int id)
            : base(pp => pp.Id == id)
        {
            // Traemos al padre para saber si es Bundle
            AddInclude(pp => pp.Product);
            // Traemos la info de la presentación para el nombre/litros
            AddInclude(pp => pp.Presentation);
        }
    }
}
