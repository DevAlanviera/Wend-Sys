namespace WendlandtVentas.Web.Models.TableModels
{
    public class InventoryTableModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Presentation { get; set; }
        public double Liters { get; set; }
        public string Stock { get; set; }

        public object Batches { get; set; }
    }
}