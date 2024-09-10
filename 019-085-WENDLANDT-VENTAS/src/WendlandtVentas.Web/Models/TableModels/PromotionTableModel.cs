namespace WendlandtVentas.Web.Models.TableModels
{
    public class PromotionTableModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Buy { get; set; }
        public int Present { get; set; }
        public string Discount { get; set; }
        public string Classification { get; set; }
        public string Presentations { get; set; }
        public string Clients { get; set; }
        public string Type { get; set; }
        public bool IsActive { get; set; }
    }
}