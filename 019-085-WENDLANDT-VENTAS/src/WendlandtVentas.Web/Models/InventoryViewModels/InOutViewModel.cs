using System;
using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.InventoryViewModels
{
    public class InOutViewModel
    {
        [Display(Name = "Cantidad")]
        [Required(ErrorMessage = "Campo requerido")]
        public int Quantity { get; set; }
        public int ProductPresentationId { get; set; }

        [Display(Name = "Comentario")]
        public string Comment { get; set; }

        public bool IsAdjustment { get; set; }

        [Display(Name = "Número de Lote")]
        [Required(ErrorMessage = "El número de lote es requerido")]
        public string BatchNumber { get; set; }

        [Display(Name = "Fecha de Caducidad")]
        [Required(ErrorMessage = "La fecha de caducidad es requerida")]
        public DateTime? ExpiryDate { get; set; }


    }
}