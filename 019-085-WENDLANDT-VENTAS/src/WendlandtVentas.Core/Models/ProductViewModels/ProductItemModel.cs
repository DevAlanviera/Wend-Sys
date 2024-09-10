
namespace WendlandtVentas.Core.Models.ProductViewModels
{
    public class ProductItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PresentationLiters { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsPresent { get; set; }
        public decimal Total => !IsPresent ? Price * Quantity : 0;
    }
}