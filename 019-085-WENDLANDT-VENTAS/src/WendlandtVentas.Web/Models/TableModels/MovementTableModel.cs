namespace WendlandtVentas.Web.Models.TableModels
{
    public class MovementTableModel
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public int QuantityCurrent { get; set; }
        public int QuantityOld { get; set; }
        public string Operation { get; set; }
        public string Comment { get; set; }
        public string CreatedAt { get; set; }
        public string User { get; set; }
    }
}