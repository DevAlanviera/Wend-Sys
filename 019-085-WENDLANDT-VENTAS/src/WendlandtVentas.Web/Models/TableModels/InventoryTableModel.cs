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

        public int SortPriority { get; set; } // <--- Agrega esto

        // Cambiamos 'object' por una lista real
        public List<BatchRowModel> Batches { get; set; } = new List<BatchRowModel>();

        // Nueva propiedad para apartados
        public List<ReservationRowModel> Reservations { get; set; } = new List<ReservationRowModel>();
    }

    public class BatchRowModel
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; }
        public int CurrentQuantity { get; set; }
        public string ExpiryDateFormatted { get; set; }
        public string StatusText { get; set; }
        public string StatusColor { get; set; }
    }

    public class ReservationRowModel
    {
        public int Id { get; set; }
        public string ClientName { get; set; }
        public int ReservedQuantity { get; set; }
        public int UsedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; }
        public string StatusColor { get; set; }
        public string ExpirationDate { get; set; }
    }
}