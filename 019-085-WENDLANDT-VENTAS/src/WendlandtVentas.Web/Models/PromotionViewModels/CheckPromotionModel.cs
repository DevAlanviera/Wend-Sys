using System.Collections.Generic;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Web.Models.PresentationViewModels;

namespace WendlandtVentas.Web.Models.PromotionViewModels
{
    public class CheckPromotionModel
    {
        public int ClientId { get; set; }
        public IEnumerable<PresentationQuantity> PresentationQuantities { get; set; }
        public CurrencyType CurrencyType { get; set; }
    }
}