namespace WendlandtVentas.Web.Models.ReportViewModels
{
    public class PivotDataOrderModel
    {
        public int OrderId { get; set; }
        public string Order { get; set; }
        public string Type { get; set; }
        public int ProductId { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public double Liters { get; set; }
        public decimal TotalProduct { get; set; }
        public string Client { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public int PresentationId { get; set; }
        public string Presentation { get; set; }
        public string CreationDate { get; set; }
        public int Month { get; set; }
        public string User { get; set; }
    }
}