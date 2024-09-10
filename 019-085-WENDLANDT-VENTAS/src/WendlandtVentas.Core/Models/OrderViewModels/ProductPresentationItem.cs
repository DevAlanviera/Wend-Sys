namespace WendlandtVentas.Core.Models
{
    public class ProductPresentationItem
    {
        public int Id { get; set; }
        public int PresentationId { get; set; }
        public int ProductPresentationId { get; set; }
        public string PresentationName { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string PriceString { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsPresent { get; set; }
        public decimal SubtotalDouble { get; set; }
        public string Subtotal { get; set; }
        public bool ExistPresentation { get; set; }
        public bool IsSeason { get; set; }
        public bool CanDelete { get; set; }
    }
}