using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.InventoryViewModels
{
    public class EditReservationViewModel
    {
        public int Id { get; set; }
        public string ClientName { get; set; }
        public string ProductName { get; set; }
        public string PresentationName { get; set; }
        public int UsedQuantity { get; set; }

        [Required(ErrorMessage = "Ingrese la cantidad")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero")]
        public int ReservedQuantity { get; set; }

        public string Notes { get; set; }
    }
}
