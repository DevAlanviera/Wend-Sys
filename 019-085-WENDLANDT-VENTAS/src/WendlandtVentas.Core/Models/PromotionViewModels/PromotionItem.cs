using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Models.ProductViewModels;

namespace WendlandtVentas.Core.Models.PromotionViewModels
{
    public class PromotionItemModel
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public int Buy { get; set; }
        public int Present { get; set; }
        public int TotalBuy => Buy + Present;
        public double Discount { get; set; }
        public decimal DiscountMoney => Products.Sum(c => c.Total);
        public int PresentationId { get; set; }
        public List<ProductItemModel> Products { get; set; } = new List<ProductItemModel>();
    }
}