using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.InventoryViewModels
{
    public class ClientReservationViewModel
    {
        public int ProductPresentationId { get; set; }
        public string ProductName { get; set; }
        public string PresentationName { get; set; }
        public double Liters { get; set; }
        public int AvailableStock { get; set; }

        [Required(ErrorMessage = "Seleccione un cliente")]
        [Display(Name = "Cliente")]
        public int ClientId { get; set; }
        public SelectList Clients { get; set; }

        [Required(ErrorMessage = "Ingrese la cantidad a apartar")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero")]
        [Display(Name = "Cantidad a apartar")]
        public int ReservedQuantity { get; set; }

        [Display(Name = "Notas / Observaciones")]
        public string Notes { get; set; }

        [Display(Name = "Fecha de expiración")]
        public string ExpirationDate { get; set; }
    }
}
