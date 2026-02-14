using System.Collections.Generic;

namespace WendlandtVentas.Web.Models.TableModels
{
    public class InventoryTableModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Presentation { get; set; }
        public double Liters { get; set; }
        public string Stock { get; set; }

        // Cambiamos 'object' por una lista real
        public List<BatchRowModel> Batches { get; set; } = new List<BatchRowModel>();
    }

    public class BatchRowModel
    {
        public string BatchNumber { get; set; }
        public int CurrentQuantity { get; set; }
        public string ExpiryDateFormatted { get; set; }
        public string StatusText { get; set; }
        public string StatusColor { get; set; }
    }
}