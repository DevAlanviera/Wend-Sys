using System.Collections.Generic;
using System.Linq;

namespace WendlandtVentas.Web.Models.PresentationViewModels
{
    public class PresentationQuantity
    {
        public int PresentationId { get; set; }
        public int Quantity => ProductQuantities.Sum(c => c.Quantity);
        public bool HasPossibilityPromotion { get; set; }
        public IEnumerable<ProductQuantity> ProductQuantities { get; set; } = new List<ProductQuantity>();
    }
}