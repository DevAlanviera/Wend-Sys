using System;
using System.Collections.Generic;
using System.Text;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.ProductSpecifications
{
    public class ProductPresentationForAlertSpecification : BaseSpecification<ProductPresentation>
    {
        public ProductPresentationForAlertSpecification(int id) : base(c => c.Id == id)
        {
            AddInclude(c => c.Product);
            AddInclude(c => c.Presentation); // Específicamente para los litros
        }
    }
}
