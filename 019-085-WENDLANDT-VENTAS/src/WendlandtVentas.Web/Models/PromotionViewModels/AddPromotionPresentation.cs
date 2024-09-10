using System.Collections.Generic;
using WendlandtVentas.Web.Models.PresentationViewModels;

namespace WendlandtVentas.Web.Models.PromotionViewModels
{
    public class AddPromotionPresentationModel
    {
        public int ClientId { get; set; }
        public IEnumerable<PresentationQuantity> PresentationQuantities { get; set; }
    }
}