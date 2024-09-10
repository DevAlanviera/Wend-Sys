using System.Collections.Generic;
using System.Linq;

namespace WendlandtVentas.Core.Models.PromotionViewModels
{
    public class PresentationPromotionModel
    {
        public string Presentation { get; set; }
        public int PresentationId { get; set; }   
        public int Quantity { get; set; }
        public decimal Discount => Promotions.SelectMany(c => c.Products).Sum(d => d.Total);
        public IEnumerable<PromotionItemModel> Promotions { get; set; }
    }
}